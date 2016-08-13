#nowarn "52"

namespace BucklingSprings.Aware.Common.Summarizers

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Common.Totals
open BucklingSprings.Aware.Common.DayStartAndEndTimes
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Core.ActivitySamples


module DailySummarizer =

    let summarizerSetVersion (classfierVersions : int list) = 33 + (List.sum classfierVersions)

    let total (samples : WithDate<MeasureForClass<ActivitySummary>> list) (d : System.DateTimeOffset) :   WithDate<MeasureByClass<ActivitySummary>> = 
        let t = samples
                    |> List.map (Dated.unWrap >> ForClass.unWrap)
                    |> List.sum
        Dated.mkDated (MeasureByClass.TotalAcrossClasses t) d

    let total' (samples : WithDate<ActivitySummary> list) (d : System.DateTimeOffset) :   WithDate<MeasureByClass<ActivitySummary>> = 
        let t = samples
                    |> List.map Dated.unWrap
                    |> List.sum
        Dated.mkDated (MeasureByClass.TotalAcrossClasses t) d

    (*
    An activity sample is never longer than an hour but the start time might be a different hour from the end time
    so it is important to that the end time and not the start time to allocate the sample to an hour.
    The start time might be in as much as a minute into the previous hour but no further

    Also take care of an edge case in Hour of work calculation where the start dt
    is top of the hour and the length is 60 minutes - we dont want to assign
    that to the next hour
    *)
    let forHour (h : int) (s : WithDate<ActivitySummary>) : bool =
        let dt, summ = s
        let seconds = float (summ.minuteCount * 60 - 1)
        let endDt = dt.AddSeconds seconds
        endDt.Hour = h

    let forHour' (h : int) (s : WithDate<MeasureForClass<ActivitySummary>>) : bool =
        let dt, (_, summ) = s
        let seconds = float (summ.minuteCount * 60 - 1)
        let endDt = dt.AddSeconds seconds
        endDt.Hour = h

    
    let totalForHour (samples : WithDate<MeasureForClass<ActivitySummary>> list) (d : System.DateTimeOffset) (h : int) :   WithDate<MeasureByClass<ActivitySummary>> = 
        let xs = samples |> List.filter (forHour' h)
        let d' = d.AddHours(float h)
        total xs d'

    let totalForHour' (samples : WithDate<ActivitySummary> list) (d : System.DateTimeOffset) (h : int) :   WithDate<MeasureByClass<ActivitySummary>> = 
        let xs = samples |> List.filter (forHour h)
        let d' = d.AddHours(float h)
        total' xs d'

    let totalsByClasses(classes : ClassificationClass list)  (samples : WithDate<MeasureForClass<ActivitySummary>> list) (d : System.DateTimeOffset) :   WithDate<MeasureByClass<ActivitySummary>> list = 
        let xs = samples |> List.map Dated.unWrap
        ClassificationForClassMeasurement.filterMapForClasses classes List.sum xs
            |> List.map (ForClass.mkByClass >> (fun x -> Dated.mkDated x d))

    let totalByClassesForHour (classes : ClassificationClass list) (samples : WithDate<MeasureForClass<ActivitySummary>> list) (d : System.DateTimeOffset) (h : int) :   WithDate<MeasureByClass<ActivitySummary>> list = 
        let xs = samples |> List.filter (forHour' h)
        let d' = d.AddHours(float h)
        totalsByClasses classes xs d'
        
    
    let minutes (x : ActivitySummary) = x.minuteCount
    let split (x : ActivitySummary) (m1 : int) (m2 : int) : ActivitySummary * ActivitySummary =
        let m = float (m1 + m2)
        let w = float x.wordCount
        let w1 = (((float m1) / m) * w) |> round |> int
        let w2 = (((float m2) / m) * w) |> round |> int
        ({ActivitySummary.minuteCount = m1; ActivitySummary.wordCount = w1}, {ActivitySummary.minuteCount = m2; ActivitySummary.wordCount = w2})

    let splitForClass x m1 m2 =
        let c, s = x
        let x1, x2 = split s m1 m2
        (c, x1), (c, x2)

    let acrossClassSummaries (d : System.DateTimeOffset) samples =
        
        let typedSamples = TypedActivitySamples.asTyped' samples
        let withPeriod p x = (p,x)
        let dayTotal : SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>> = withPeriod SummaryTimePeriod.Day (total' typedSamples d)
        let hours = [for i in 0..23 -> i]
        let hourlyTotals = hours
                                |> List.map (totalForHour' typedSamples d)
                                |> List.map (withPeriod SummaryTimePeriod.HourOfDay)
        let hourOfWorkSamples = HourOfWorkCalculator.assignToHourOfDays d (typedSamples |> List.map Dated.unWrap)
        let hourOfWorkTotals = hours
                                |> List.map (totalForHour' hourOfWorkSamples d)
                                |> List.map (withPeriod SummaryTimePeriod.HourOfWork)

        List.concat [[dayTotal]; hourlyTotals; hourOfWorkTotals]

    let summarizeForClassifier (d : System.DateTimeOffset) samples (classifier : Classifier) =


        let classes = classifier.classes |> Seq.toList
        let classIds = classes |> Seq.map (fun c -> ClassIdentifier c.id) |> Seq.toList
        let classIdSet = Set(classIds)
        let typedSamples = TypedActivitySamples.asTyped classIdSet samples

        assert (d.EqualsExact(d.StartOfDay))
        let withPeriod p x = (p,x)
        let dayClassTotals : (SummaryTimePeriod * WithDate<MeasureByClass<ActivitySummary>>) list = totalsByClasses classes typedSamples d |> List.map (withPeriod SummaryTimePeriod.Day)

        let hours = [for i in 0..23 -> i]
        let hourlyClassTotals = hours
                                    |> List.map (totalByClassesForHour classes typedSamples d)
                                    |> List.concat
                                    |> List.map (withPeriod SummaryTimePeriod.HourOfDay)

        let hourOfWorkSamples = 
            let undated = typedSamples |> List.map Dated.unWrap
            let injectClass (clsId : ClassIdentifier) (x : WithDate<ActivitySummary>) : WithDate<MeasureForClass<ActivitySummary>> =
                let dt, e = x
                Dated.mkDated (clsId, e) dt
            let samplesForClass clsId = 
                    undated
                        |> List.choose (ForClass.chooseForClass clsId)
                        |> HourOfWorkCalculator.assignToHourOfDays d
                        |> List.map (injectClass clsId)
                        
            classIds
                |> List.collect samplesForClass

            

        let hourOfWorkClassTotals = hours
                                    |> List.map (totalByClassesForHour classes hourOfWorkSamples d)
                                    |> List.concat
                                    |> List.map (withPeriod SummaryTimePeriod.HourOfWork)
        
        List.concat [dayClassTotals; hourlyClassTotals; hourOfWorkClassTotals]
        
    
    let summarize (d : System.DateTimeOffset) (samples : ActivitySample list) (allClassifiers : Classifier list) (allClasses : ClassificationClass list) =

        trace "Summarizing for Day %A - %d Samples" d (List.length samples) 
        assert (d.EqualsExact(d.StartOfDay))
        
        let nonIdleSamples = samples |> List.filter (fun s -> s.inputActivity.IsNotIdle)

        let dayLength = if List.isEmpty nonIdleSamples then
                            DayLength.Zero
                        else
                            let minDt = nonIdleSamples |> List.map (fun s -> s.sampleStartTimeAndDate) |> List.min
                            let maxDt = nonIdleSamples |> List.map (fun s -> s.sampleEndTimeAndDate) |> List.max
                            {
                                DayLength.startTime = StartAndEndTime.fromDtTime minDt;
                                DayLength.endTime = StartAndEndTime.fromDtTime maxDt;
                            }

        let actSumTotal = acrossClassSummaries d nonIdleSamples
        let actSums = allClassifiers |> List.map (summarizeForClassifier d nonIdleSamples) |> List.concat
        let versions = allClassifiers |> List.map (fun c -> c.classifierDefinitionVersion)
        let v = summarizerSetVersion versions

        TypedStoredSummaries.write
                                    (Map(allClasses |> Seq.map (fun c -> (ClassIdentifier c.id, c))))
                                    (
                                        List.concat [actSums; actSumTotal],
                                        [Dated.mkDated v d],
                                        [Dated.mkDated dayLength d]
                                    )
