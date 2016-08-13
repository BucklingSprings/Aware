#nowarn "52"

namespace BucklingSprings.Aware.Store


open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Classifiers
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Environment
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.ActivitySamples
open BucklingSprings.Aware.Entitities

open System
open System.Data.Entity
open System.Data.Entity.Migrations
open System.Data
open System.Collections.Generic
open System.Linq

module Store =

    
    let connectionString = Environment.connectionString
 
    let initialize (upgrade) =
        
        
        if upgrade then
            System.Data.Entity.Database.SetInitializer(MigrateDatabaseToLatestVersion<AwareContext, MigrationConfiguration>())


        use awareStore = new AwareContext(connectionString)
        awareStore.Database.Initialize(false) |> ignore
        ()


module SearchStore =


    let searchSqlBase (t : string) =
        let s = """
             SELECT
                            COALESCE(SUM(DATEDIFF(SECOND, a.sampleStartTimeAndDate, a.sampleEndTimeAndDate)),0) as seconds,
                            COALESCE(SUM(a.inputActivity_keyboardActivity),0) as keyboardActivity
                         FROM ActivitySamples a
                            INNER JOIN ActivityWindowDetails awd ON awd.id = a.activityWindowDetail_id
                                WHERE (a.inputActivity_keyboardActivity > 0 OR a.inputActivity_mouseActivity > 0)"""
        sprintf "%s AND windowText Like '%%%s%%'" s t

    let activityTime (text : string) =
        use awareStore = new AwareContext(Store.connectionString)
        let sql = searchSqlBase text
        let results = awareStore.Database.SqlQuery<SearchResult>(sql)
        let results = Seq.toList results
        let x = Seq.head results // exactly one result expected 
        x
    
    let activityTimeForDateRange (startDt : System.DateTimeOffset) (endDt : System.DateTimeOffset) (text : string) =
        use awareStore = new AwareContext(Store.connectionString)
        let s = searchSqlBase text
        let sql = sprintf "%s  AND a.sampleStartTimeAndDate > = {0} AND a.sampleEndTimeAndDate <= {1}" s
        let results = awareStore.Database.SqlQuery<SearchResult>(sql, startDt, endDt)
        let x = Seq.head results // exactly one result expected 
        x
    
module ActivitySamplesStore =

    let earliestSampleTimeAndDate  () =
        use awareStore = new AwareContext(Store.connectionString)
        let samples = 
            (query {
                for s in awareStore.ActivitySamples do
                sortBy s.sampleStartTimeAndDate
                select s
                take 1
            }) |> Seq.toList
        if List.isEmpty samples then
            None
        else
            Some (List.head samples).sampleStartTimeAndDate

    let processAllInDateRange startDt endDt (f : ActivitySample seq -> 'a)  =
        use awareStore = new AwareContext(Store.connectionString)
        let samples = 
            query {
                for s in awareStore.ActivitySamples.Include(fun (s : ActivitySample) -> s.classes).Include(fun (s : ActivitySample) -> s.activityWindowDetail).Include(fun (s : ActivitySample) -> s.activityWindowDetail.processInformation).Include(fun (s: ActivitySample) -> s.classes.Select(fun (sca: SampleClassAssignment) -> sca.assignedClass.classifier))  do
                where (s.sampleStartTimeAndDate >= startDt && s.sampleEndTimeAndDate <= endDt)
                sortBy s.sampleStartTimeAndDate
                select s
            }
        f samples
        

    let processAllAsUnsavedSamplesInOrder (f : UnsavedSample -> Unit) =
        use awareStore = new AwareContext(Store.connectionString)
        let samples = 
            query {
                for s in awareStore.ActivitySamples.Include(fun (s : ActivitySample) -> s.activityWindowDetail).Include(fun (s: ActivitySample) -> s.activityWindowDetail.processInformation)  do
                sortBy s.sampleStartTimeAndDate
                select s
            }
        samples |> Seq.iter (fun s ->
                                let us = {
                                            UnsavedSample.inputActivity = s.inputActivity
                                            UnsavedSample.processName = s.activityWindowDetail.processInformation.processName
                                            UnsavedSample.timeAndDate =  s.sampleStartTimeAndDate
                                            UnsavedSample.windowTitle = s.activityWindowDetail.windowText
                                            }
                                f(us))
        ()
    
    let getLastNDaysOfActivities n (endDt : System.DateTimeOffset) =
        let startDt = endDt.SubtractDays(n-1).StartOfDay
        use awareStore = new AwareContext(Store.connectionString)
        let samples = 
            query {
                for s in awareStore.ActivitySamples.Include(fun (s : ActivitySample) -> s.activityWindowDetail).Include(fun (s: ActivitySample) -> s.activityWindowDetail.processInformation).Include(fun (s: ActivitySample) -> s.classes.Select(fun (sca: SampleClassAssignment) -> sca.assignedClass.classifier))  do
                where (s.sampleStartTimeAndDate >= startDt && s.sampleEndTimeAndDate <= endDt)
                select s
            }
        List.ofSeq samples

    let samplesForDay (d : System.DateTimeOffset) = getLastNDaysOfActivities 1 (d.EndOfDay)

    let lastNSamples (n : int) =
        use awareStore = new AwareContext(Store.connectionString)
        let samples = 
            query {
                for s in awareStore.ActivitySamples.Include(fun (s : ActivitySample) -> s.activityWindowDetail).Include(fun (s: ActivitySample) -> s.activityWindowDetail.processInformation)  do
                sortByDescending s.id
                select s
                take n
            }
        List.ofSeq samples
        

module SampleStore = 

    let samplesNeedingAssignmentBatch (c : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let ids = ctx.Database.SqlQuery<int>("""SELECT TOP 200 s.id
                                                FROM ActivitySamples s
                                                    LEFT OUTER JOIN SampleClassAssignments sca ON sca.ActivitySample_id = s.id AND sca.classifierIdentifier = {0}
                                                    WHERE sca.id IS NULL OR sca.classifierDefinitionVersion <> {1}""", c.id, c.classifierDefinitionVersion) |> Seq.toList
        let samples = 
            (query {
                for s in ctx.ActivitySamples.Include(fun (s : ActivitySample) -> s.activityWindowDetail).Include(fun (s: ActivitySample) -> s.activityWindowDetail.processInformation).Include(fun (s: ActivitySample) -> s.classes.Select(fun (sca: SampleClassAssignment) -> sca.assignedClass)) do
                where (ids.Contains(s.id))
                select s
            }) |> Seq.toList
        samples

    let processByNameOrNew (ctx : AwareContext) nm = 
        let p =
            query {
                for p in ctx.Processes do
                where (p.processName = nm)
                select p
                exactlyOneOrDefault
            }
        if (p = null) then
            ctx.Processes.Add(Process(processName = nm))
        else
            p

    let windowDetailByTitleAndProcessOrNew (ctx :AwareContext) title (prc : Process) =
        let createNew _ = ctx.WindowDetails.Add(ActivityWindowDetail(windowText = title, processInformation = prc))
        if (prc.id = 0) then
            createNew()
        else
            let w = query {
                for w in ctx.WindowDetails.Include(fun (w : ActivityWindowDetail) -> w.processInformation) do
                where (w.windowText = title && w.processInformation.id = prc.id)
                select w
                exactlyOneOrDefault
            }
            if (w = null) then
                createNew()
            else
                w


    let add (sample : UnsavedSample) =
        use awareStore = new AwareContext(Store.connectionString)
        let startTime = sample.timeAndDate.Subtract(System.TimeSpan.FromMilliseconds(float SystemConfiguration.sampleTimeInMilliseconds))
        let prc = processByNameOrNew awareStore sample.processName
        let wind = windowDetailByTitleAndProcessOrNew awareStore sample.windowTitle prc
        let activitySample = awareStore.ActivitySamples.Add(ActivitySample(sampleStartTimeAndDate = startTime, sampleEndTimeAndDate = sample.timeAndDate, inputActivity = sample.inputActivity, activityWindowDetail = wind))
        awareStore.SaveChanges() |> ignore        
        activitySample           
        
    
    let incrementActivity (actSample : ActivitySample) (sample : UnsavedSample) =
        use awareStore = new AwareContext(Store.connectionString)
        let stored = awareStore.ActivitySamples.Find(actSample.id)
        stored.inputActivity <- sample.inputActivity
        stored.sampleEndTimeAndDate <- sample.timeAndDate
        awareStore.SaveChanges() |> ignore
        stored

    let save (sample : UnsavedSample) (previous : (ActivitySample * UnsavedSample) option) : (bool * (ActivitySample * UnsavedSample)) =
        if Option.isNone previous then
            let actSamp = add sample
            trace "Create New Sample %s" sample.windowTitle
            (true, (actSamp, sample))
        else
            let actSamp, prev = Option.get previous
            let combination = SampleCombiner.combine prev sample
            match combination with
            | SampleCombiner.SampleCombinationResult.CombinedSample s ->
                trace "Combined Sample %s" sample.windowTitle
                let actS = incrementActivity actSamp s
                (false ,(actS, s))
            | SampleCombiner.SampleCombinationResult.NewSample reason ->
                trace "Create New Sample %s %A" sample.windowTitle reason
                let actSamp = add sample
                (true, (actSamp, sample))
            



module ExampleStore =

    type IdCountPair() =
        member val Id = 0 with get, set
        member val Count = 0 with get, set

    let nextExample (model : ClassifierModel) : (ActivityWindowDetail * int) option =
        use ctx = new AwareContext(Store.connectionString)
        let ids = ctx.Database.SqlQuery<IdCountPair>("""select top 1 ActivityWindowDetails.id, Count(*) as Count
                                                        from ActivityWindowDetails
                                                            inner join Processes
                                                                on Processes.id = ActivityWindowDetails.processInformation_id
                                                            inner join ActivitySamples
                                                                on ActivitySamples.activityWindowDetail_id = ActivityWindowDetails.id
                                                                and ActivitySamples.inputActivity_mouseActivity > 0 And ActivitySamples.inputActivity_keyboardActivity > 0
                                                            left join ClassifierModelExamples
                                                                on ClassifierModelExamples.windowDetail_id = ActivityWindowDetails.id And ClassifierModelExamples.model_id = {0}
                                                        Where ClassifierModelExamples.id is NULL Or ClassifierModelExamples.trained = 0
                                                        group by ActivityWindowDetails.id, ActivityWindowDetails.windowText, Processes.processName
                                                        Order by Count(*) desc""", model.id) |> Seq.toList
        if List.isEmpty ids then
            None
        else
            let idC = List.head ids
            Some (ctx.WindowDetails.Include(fun (w : ActivityWindowDetail) -> w.processInformation).First(fun (w : ActivityWindowDetail) -> w.id = idC.Id), idC.Count)


    let markAsTrained (model : ClassifierModel) (w : ActivityWindowDetail) =
        use ctx = new AwareContext(Store.connectionString)
        let model = ctx.ClassifierModels.Attach(model)
        let w = ctx.WindowDetails.Attach(w)
        let ex = {
                    ClassifierModelExample.id = 0
                    ClassifierModelExample.model = model
                    ClassifierModelExample.trained = 1
                    ClassifierModelExample.windowDetail = w
                 }
        ctx.ClassifierModelExamples.Add(ex) |> ignore
        ctx.SaveChanges() |> ignore

    let markAsUnTrained (model : ClassifierModel) (w : ActivityWindowDetail) =
        use ctx = new AwareContext(Store.connectionString)
        let model = ctx.ClassifierModels.Attach(model)
        let w = ctx.WindowDetails.Attach(w)
        let examples =
            (query {
                for cme in ctx.ClassifierModelExamples do
                where (cme.windowDetail.id = w.id && cme.model.id = model.id)
                select cme
                take 1
            })  |> Seq.toList
        examples
            |> List.iter (fun e -> ctx.ClassifierModelExamples.Remove(e) |> ignore)
        ctx.SaveChanges() |> ignore


    
module ModelStore =

    let create () =
        use awareStore = new AwareContext(Store.connectionString)
        let m = ClassifierModel(createdOn = System.DateTimeOffset.Now)
        let m = awareStore.ClassifierModels.Add(m)
        awareStore.SaveChanges() |> ignore
        m

    let modelFileName (m : ClassifierModel) =
        let fileName = sprintf "Classifier%d.aware.model" m.id
        System.IO.Path.Combine(BucklingSprings.Aware.Core.Environment.modelLocation, fileName)

module HourlyTotalsStore =


    let totalsForDateRage (startDt : System.DateTimeOffset) (endDt : System.DateTimeOffset) (f : HourlyTotal -> Unit) =
        let s = """
            SELECT
		            CONVERT(date,a.sampleStartTimeAndDate) as sampleDate, DATEPART(HOUR, a.sampleEndTimeAndDate) as sampleHour,
		            COALESCE(SUM(DATEDIFF(SECOND, a.sampleStartTimeAndDate, a.sampleEndTimeAndDate)),0) as seconds,
		            COALESCE(SUM(a.inputActivity_keyboardActivity),0) as keyboardActivity
	        FROM ActivitySamples a
            WHERE 
                (a.inputActivity_keyboardActivity > 0 Or a.inputActivity_mouseActivity > 0)
                AND (a.sampleStartTimeAndDate > = {0} AND a.sampleEndTimeAndDate <= {1})
            GROUP BY CONVERT(date,a.sampleStartTimeAndDate), DATEPART(HOUR, a.sampleEndTimeAndDate) 
	        ORDER BY CONVERT(date,a.sampleStartTimeAndDate), DATEPART(HOUR, a.sampleEndTimeAndDate)
                """
        use awareStore = new AwareContext(Store.connectionString)
        awareStore.Database.SqlQuery<HourlyTotal>(s, startDt, endDt)
            |> Seq.iter f


module DailyTotalsStore =


    let dailyTotalsForDateRage (startDt : System.DateTimeOffset) (endDt : System.DateTimeOffset) =
        let s = """
            SELECT
		        CONVERT(date,a.sampleStartTimeAndDate) as sampleDate,
		        COALESCE(SUM(DATEDIFF(SECOND, a.sampleStartTimeAndDate, a.sampleEndTimeAndDate)),0) as seconds,
		        COALESCE(SUM(a.inputActivity_keyboardActivity),0) as keyboardActivity
	        FROM ActivitySamples a
            WHERE 
                (a.inputActivity_keyboardActivity > 0 Or a.inputActivity_mouseActivity > 0)
                AND (a.sampleStartTimeAndDate > = {0} AND a.sampleEndTimeAndDate <= {1})
            GROUP BY CONVERT(date,a.sampleStartTimeAndDate)
	        ORDER BY CONVERT(date,a.sampleStartTimeAndDate)
                """
        use awareStore = new AwareContext(Store.connectionString)
        awareStore.Database.SqlQuery<DailyTotal>(s, startDt, endDt)
            |> Seq.toList

    let dailyPerformanceUsingDateRange (startDt : System.DateTimeOffset) (endDt : System.DateTimeOffset) =
        let toMins s =
            let ts = TimeSpan.FromSeconds(s)
            int ts.TotalMinutes
        let toWords k =
            TypedActivitySamples.keyStrokesToWords (int k)
        let totals = dailyTotalsForDateRage startDt endDt
        if List.isEmpty totals then
            {words = 0; minutes = 0}
        else
            let medianActivity = (totals
                            |> List.map (fun t -> t.keyboardActivity)
                            |> Statistics.StatisticalSummary.summarize).median
            let medianSeconds = (totals
                            |> List.map (fun t -> t.seconds)
                            |> Statistics.StatisticalSummary.summarize).median
            {words = toWords medianActivity; minutes = toMins medianSeconds}

    let historicalPerformanceUsingDateRange (startDt : System.DateTimeOffset) (endDt : System.DateTimeOffset) =
        let perf = dailyPerformanceUsingDateRange startDt endDt
        if perf.minutes = 0 && perf.words = 0 then
            {words = 2500; minutes = 360}
        else
            perf
        
        