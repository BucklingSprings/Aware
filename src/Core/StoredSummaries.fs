namespace BucklingSprings.Aware.Core.StoredSummaries

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.DayStartAndEndTimes

[<RequireQualifiedAccess()>]
type SummaryTimePeriod =
    | Day
    | HourOfDay
    | HourOfWork

[<RequireQualifiedAccess()>]
type SummaryMeasurementType =
    | ActivitySummary
    | SummarySetVersion
    | StartAndEndTimeInMinutesFromDayStart

[<RequireQualifiedAccess()>]
type SummarySpan =
    | ByClass
    | AcrossClasses

type SummaryType = SummaryType of SummaryTimePeriod * SummaryMeasurementType * SummarySpan

module StoredSummaries =

    let activitySummaryBy span = SummaryType(SummaryTimePeriod.Day, SummaryMeasurementType.ActivitySummary, span)
    let summaryCalculatedBy span = SummaryType(SummaryTimePeriod.Day, SummaryMeasurementType.SummarySetVersion, span)
    let startAndEndTimeBy span = SummaryType(SummaryTimePeriod.Day, SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart, span)
    let hourOfDayActivitySummary span = SummaryType(SummaryTimePeriod.HourOfDay, SummaryMeasurementType.ActivitySummary, span)
    let hourOfWorkActivitySummary span = SummaryType(SummaryTimePeriod.HourOfWork, SummaryMeasurementType.ActivitySummary, span)

    let summaryCalculatedSummary = summaryCalculatedBy SummarySpan.AcrossClasses
    let activitySummaryByDay = activitySummaryBy SummarySpan.AcrossClasses
    let activitySummaryByClass = activitySummaryBy SummarySpan.ByClass

    let hourOfDayActivitySummaryTotal  = hourOfDayActivitySummary SummarySpan.AcrossClasses
    let hourOfDayActivitySummaryByClass  = hourOfDayActivitySummary SummarySpan.ByClass

    let hourOfWorkActivitySummaryTotal  = hourOfWorkActivitySummary SummarySpan.AcrossClasses
    let hourOfWorkActivitySummaryByClass  = hourOfWorkActivitySummary SummarySpan.ByClass


    let startAndEndTimeDay = startAndEndTimeBy SummarySpan.AcrossClasses

    let machinizeTimePeriod =
        function
            | SummaryTimePeriod.Day -> "Day"
            | SummaryTimePeriod.HourOfDay -> "HourOfDay"
            | SummaryTimePeriod.HourOfWork -> "HourOfWork"

    let unmachinizeTimePeriod s =
        match s with
        | "Day" -> SummaryTimePeriod.Day
        | "HourOfDay" -> SummaryTimePeriod.HourOfDay
        | "HourOfWork" -> SummaryTimePeriod.HourOfWork
        | _ -> failwith (sprintf "Unexpected Time Period %s" s)

    let machinizeMeasurementType =
        function 
            | SummaryMeasurementType.ActivitySummary -> "ActivitySummary"
            | SummaryMeasurementType.SummarySetVersion -> "SummarySetVersion"
            | SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart -> "StartEndTimeDay"

    let unmachinizeMeasurementType s =
        match s with
        | "ActivitySummary" -> SummaryMeasurementType.ActivitySummary
        | "SummarySetVersion" -> SummaryMeasurementType.SummarySetVersion
        | "StartEndTimeDay" -> SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart
        | _ -> failwith (sprintf "Unexpected Measurement Type %s" s)

    let machinizeSummarySpan =
        function 
            | SummarySpan.AcrossClasses -> "AcrossClasses"
            | SummarySpan.ByClass -> "ByClass"

    let unmachinizeSummarySpan s =
        match s with
        | "AcrossClasses" -> SummarySpan.AcrossClasses
        | "ByClass" -> SummarySpan.ByClass
        | _ -> failwith (sprintf "Unexpected Span %s" s)

    let allTypes : SummaryType list = 
        seq {
            for p in [SummaryTimePeriod.Day; SummaryTimePeriod.HourOfDay; SummaryTimePeriod.HourOfWork] do
                for m in [SummaryMeasurementType.ActivitySummary; SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart; SummaryMeasurementType.SummarySetVersion] do
                    for s in [SummarySpan.ByClass; SummarySpan.AcrossClasses] do
                        yield SummaryType(p,m,s)} |> Seq.toList

    let machinize (SummaryType(t, m, c)) = sprintf "%s_%s_%s" (machinizeTimePeriod t) (machinizeMeasurementType m) (machinizeSummarySpan c)

    let unmachinize (s : string) : SummaryType =
        let parts = s.Split [| '_' |]
        assert (parts.Length = 3)
        SummaryType(unmachinizeTimePeriod (parts.[0]), unmachinizeMeasurementType (parts.[1]), unmachinizeSummarySpan (parts.[2]))

    let isDaily (SummaryType(p,_,_)) =
        match p with
        | SummaryTimePeriod.Day -> true
        | SummaryTimePeriod.HourOfWork -> true
        | SummaryTimePeriod.HourOfDay -> true

    let allDailySummaryTypes =  List.filter isDaily allTypes

    let allDailySummaryTypesMachinized = allDailySummaryTypes |> List.map machinize


module StoredSummariesSerializer =


    let writeActivitySummary (actSum : ActivitySummary) =
        sprintf "%d;%d" actSum.minuteCount actSum.wordCount

    let writeDayLength (dayLength : DayLength) =
        let min = StartAndEndTime.toMinutesFromStartOfDay
        sprintf "%d;%d" (min dayLength.startTime) (min dayLength.endTime)

    let writeSummarySetVersion (v : int) =
        string v
        
    let readActivitySummary (s : string) : ActivitySummary =
        let parts = s.Split(';')
        {
            ActivitySummary.minuteCount = int parts.[0]
            ActivitySummary.wordCount = int parts.[1]
        }

    let readDayLength (s : string) : DayLength =
        let parts = s.Split(';')
        {
            DayLength.startTime = TimeOfDay.FromStartOfDay (int parts.[0])
            DayLength.endTime = TimeOfDay.FromStartOfDay (int parts.[1])
        }

    let readSummarySetVersion (s : string) : int =
        int s

module TypedStoredSummaries =
 
    type TypedStoredSummaries = (SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>>) list * WithDate<int> list * WithDate<DayLength> list

    let summary d st value (classificationClassO : ClassificationClass option) =
        let cc, cVer, cId, span = 
            if Option.isNone classificationClassO then
                (null, System.Nullable<int>(), System.Nullable<int>(), SummarySpan.AcrossClasses)
            else
                let cc = Option.get classificationClassO
                (cc,System.Nullable<int>(cc.classifier.classifierDefinitionVersion),System.Nullable<int>(cc.classifier.id), SummarySpan.ByClass)
        {
            StoredSummary.id = 0
            StoredSummary.summary = value
            StoredSummary.summaryType = StoredSummaries.machinize (st span)
            StoredSummary.summaryClass = cc
            StoredSummary.summaryClassifierDefinitionVersion = cVer
            StoredSummary.summaryClassifierIdentifier = cId
            StoredSummary.summaryTimeAndDate = d
        }

    let versionToStoredSummary (x : WithDate<int>) : StoredSummary =
        let dt, version = x
        summary dt StoredSummaries.summaryCalculatedBy (StoredSummariesSerializer.writeSummarySetVersion version) None

    let dayLengthToStoredStummary (x : WithDate<DayLength>) : StoredSummary option =
        let dt, dayLength = x
        if dayLength = DayLength.Zero then
            None
        else
            Some (summary dt StoredSummaries.startAndEndTimeBy (StoredSummariesSerializer.writeDayLength dayLength) None)

    let activitySummaryToStoredStummary (classMap : Map<ClassIdentifier, ClassificationClass>) (x : SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>>) : StoredSummary option =
        let p, (dt, summByClass) = x
        let summ = ByClass.unWrap summByClass
        if summ = ActivitySummary.Zero then
            None
        else
            let by =
                match p with
                | SummaryTimePeriod.Day -> StoredSummaries.activitySummaryBy
                | SummaryTimePeriod.HourOfDay -> StoredSummaries.hourOfDayActivitySummary
                | SummaryTimePeriod.HourOfWork -> StoredSummaries.hourOfWorkActivitySummary
            let classO = ByClass.unWrap' (fun _ -> None) (fun i _ -> Some (classMap.Item(i))) summByClass
            Some (summary dt by (StoredSummariesSerializer.writeActivitySummary summ) classO)

    let activitySummary (ss : StoredSummary) p mt span : (SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>>)=
            assert (mt = SummaryMeasurementType.ActivitySummary)

            let actSum = StoredSummariesSerializer.readActivitySummary ss.summary
            let byClass = if ss.summaryClass = null then
                            assert (span = SummarySpan.AcrossClasses)
                            MeasureByClass.TotalAcrossClasses actSum
                          else
                            assert (span = SummarySpan.ByClass)
                            MeasureByClass.ForClass (ClassIdentifier ss.summaryClass.id, actSum)
            (p, Dated.mkDated byClass ss.summaryTimeAndDate)

    let version (ss : StoredSummary) p mt span : WithDate<int> =
        assert (mt = SummaryMeasurementType.SummarySetVersion)
        assert (span = SummarySpan.AcrossClasses)
        assert (p = SummaryTimePeriod.Day)

        let ver = StoredSummariesSerializer.readSummarySetVersion ss.summary
        Dated.mkDated ver ss.summaryTimeAndDate

    let dayLength (ss : StoredSummary) p mt span : WithDate<DayLength> =
        assert (mt = SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart)
        assert (span = SummarySpan.AcrossClasses)
        assert (p = SummaryTimePeriod.Day)

        let dl = StoredSummariesSerializer.readDayLength ss.summary
        Dated.mkDated dl ss.summaryTimeAndDate
    
    let read (sss : StoredSummary list) : TypedStoredSummaries =
        let folder (summs, vers, dayLengths) (ss : StoredSummary) : TypedStoredSummaries =
            let st = StoredSummaries.unmachinize ss.summaryType
            match st with
                | SummaryType(period, measurementType, span) ->
                    match measurementType with
                            | SummaryMeasurementType.ActivitySummary -> (List.Cons(activitySummary ss period measurementType span, summs), vers, dayLengths)
                            | SummaryMeasurementType.SummarySetVersion -> (summs, List.Cons(version ss period measurementType span, vers), dayLengths)
                            | SummaryMeasurementType.StartAndEndTimeInMinutesFromDayStart -> (summs, vers, List.Cons(dayLength ss period measurementType span, dayLengths))
                            
        List.fold folder ([],[],[]) sss

    let write (classMap : Map<ClassIdentifier, ClassificationClass>) (ss : TypedStoredSummaries) : StoredSummary list =
        let actSums, versions, dayLengths = ss
        List.concat [
                        (versions |> List.map versionToStoredSummary)
                        (actSums |> List.choose (activitySummaryToStoredStummary classMap));
                        (dayLengths |> List.choose dayLengthToStoredStummary)
                    ]

    let filterToPeriod (p) (sss : (SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>>) list) : WithDate<MeasureByClass<ActivitySummary>> list =
        let chooser ss = if p = fst ss then  Some (snd ss) else None
        List.choose chooser sss