#nowarn "52"

namespace BucklingSprings.Aware.Common.CatchUp


open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store


[<NoComparison()>]
type CatchUpMessage =
    | InitialClassification of ActivitySample
    | ReClassify of Classifier * ActivitySample
    | DailySummary of System.DateTimeOffset

[<NoComparison()>]
type CatchUpBatch = 
    | CaughtUp
    | Batch of CatchUpMessage list

module ClassAssignmentCatchUp =

    let batch () = 
        let classifiers = ClassifierStore.allClassifiers()
        let toMsg (c, ws) = ws |> List.map (fun w -> ReClassify(c, w))
        let needingClassification =
                                    classifiers
                                        |> List.map (fun c -> (c, SampleStore.samplesNeedingAssignmentBatch c))
                                        |> List.collect toMsg
        if List.isEmpty needingClassification then
            CatchUpBatch.CaughtUp
        else
            CatchUpBatch.Batch needingClassification
        

module DailySummaryCatchup =

    let daysNeedingSummaries summarySetVersion =
        let earliestSampleTime = ActivitySamplesStore.earliestSampleTimeAndDate()
        let lastSampleDate = System.DateTimeOffset.Now.AddHours(-30.0).StartOfDay
        if Option.isNone earliestSampleTime then
            []
        else
            let earliest = (Option.get earliestSampleTime).StartOfDay
            let totalDays = int (lastSampleDate.Subtract(earliest.StartOfDay).TotalDays)
            let allPossibleDays = [for i in 0..totalDays -> earliest.AddDays(float i)]
            let datesWithSummaries = SummaryStore.datesWithSummaries summarySetVersion
            List.filter (fun d -> not (datesWithSummaries.Contains d)) allPossibleDays
            

    let summaryCatchUp summarySetVersion : CatchUpBatch =
        let ds = daysNeedingSummaries summarySetVersion
        if List.isEmpty ds then
            CatchUpBatch.CaughtUp
        else
            CatchUpBatch.Batch (ds |> List.map CatchUpMessage.DailySummary |> List.toSeq |> Seq.truncate 25 |> Seq.toList)

    let numberOfDaysNeeding summarySetVersion =
        daysNeedingSummaries summarySetVersion
            |> List.length
        

    

