#nowarn "52"

namespace BucklingSprings.Aware.Common.DayStartAndEndTimes

open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions


module SampleStartAndEndTime =

    let dayLengthFromSamples (samples : ActivitySample list) =
        let nonIdleSamples = samples |> List.filter (fun s -> s.inputActivity.IsNotIdle)
        if List.isEmpty nonIdleSamples then
            None
        else
            let startTime = nonIdleSamples |> List.minBy (fun s -> s.sampleStartTimeAndDate) |> (fun s -> StartAndEndTime.fromDtTime s.sampleStartTimeAndDate)
            let endTime = nonIdleSamples |> List.maxBy (fun s -> s.sampleEndTimeAndDate) |> (fun s -> StartAndEndTime.fromDtTime s.sampleEndTimeAndDate)
            // FIXME Assert all samples are for the same day
            Some {
                    DayLength.startTime = startTime
                    DayLength.endTime = endTime
            }


module StoredSummariesDayLength =

    let dayLengthFromStoredSummaries (ss : StoredSummary list) =
        // ASSERT all summaries are same day
        let findOrZero st = 
            let x = ss |> List.tryFind (fun ss -> ss.summaryType = StoredSummaries.machinize st)
            if Option.isNone x then
                DayLength.Zero
            else
                let s = Option.get x
                StoredSummariesSerializer.readDayLength s.summary
        findOrZero StoredSummaries.startAndEndTimeDay
        


