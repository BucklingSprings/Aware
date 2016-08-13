namespace BucklingSprings.Aware.Core.ActivitySamples

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Summaries

(*
-        An activity sample is never longer than an hour but the start time might be a different hour from the end time
-        so it is important to that the end time and not the start time to allocate the sample to an hour.
-        The start time might be in as much as a minute into the previous hour but no further
-        *)

module TypedActivitySamples = 

    let minutes endDt (startDt : System.DateTimeOffset) =
                            let ts : System.TimeSpan = endDt - startDt
                            ts.TotalMinutes |> round |> int

    let keyStrokesToWords k = (float k / 12.0) |> round |> int

    let asTypedOption (classIds : Set<ClassIdentifier>) (sample : ActivitySample) : WithDate<MeasureForClass<ActivitySummary>> option =
        let cO = sample.classes |> Seq.tryFind (fun c -> classIds.Contains(ClassIdentifier c.assignedClass.id))
        Option.map (fun (c : SampleClassAssignment) ->
                        let a = {ActivitySummary.wordCount = keyStrokesToWords sample.inputActivity.keyboardActivity; ActivitySummary.minuteCount = minutes sample.sampleEndTimeAndDate sample.sampleStartTimeAndDate}
                        Dated.mkDated (ClassIdentifier c.assignedClass.id, a) sample.sampleStartTimeAndDate
                    ) cO

  

    let asTypedNonIdleOption' (sample : ActivitySample) : WithDate<ActivitySummary> option =
        if sample.inputActivity.IsNotIdle then
            let a = {ActivitySummary.wordCount = keyStrokesToWords sample.inputActivity.keyboardActivity; ActivitySummary.minuteCount = minutes sample.sampleEndTimeAndDate sample.sampleStartTimeAndDate}
            Some(Dated.mkDated a sample.sampleStartTimeAndDate)
        else
            None

    let asTyped (classIds : Set<ClassIdentifier>) (ss : ActivitySample list) : WithDate<MeasureForClass<ActivitySummary>> list =
        ss |> List.choose (asTypedOption classIds)

    
    let asTyped' (ss : ActivitySample list) : WithDate<ActivitySummary> list =
        ss |> List.choose asTypedNonIdleOption'

