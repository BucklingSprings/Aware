namespace BucklingSprings.Aware.Core.Summaries

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

module HourOfWorkCalculator =
    
    let chunksOf n items =
        let rec loop i acc items = 
            seq {
                match i, items, acc with
                //exit if chunk size is zero or input list is empty
                | _, [], [] | 0, _, [] -> ()
                //counter=0 so yield group and continue looping
                | 0, _, _::_ -> yield List.rev acc; yield! loop n [] items 
                //decrement counter, add head to group, and loop through tail
                | _, h::t, _ -> yield! loop (i-1) (h::acc) t
                //reached the end of input list, yield accumulated elements
                //handles items.Length % n <> 0
                | _, [], _ -> yield List.rev acc
            }
        loop n [] items

    let explode (x : ActivitySummary) : ActivitySummary list =
        let wordPerMinutei = ((float x.wordCount) / (float x.minuteCount)) |> floor |> int
        let wordCorrection = x.wordCount - (wordPerMinutei * x.minuteCount)
        assert (wordCorrection >= 0)
        let xs = [for i in 1 .. x.minuteCount -> {ActivitySummary.wordCount = wordPerMinutei; ActivitySummary.minuteCount = 1} ]
        List.Cons({List.head xs with ActivitySummary.wordCount= wordPerMinutei + wordCorrection}, List.tail xs)
        

    let assignToHourOfDays (startOfDay : System.DateTimeOffset) (samples : ActivitySummary list) : WithDate<ActivitySummary> list =
        assert (startOfDay.EqualsExact(startOfDay.StartOfDay))

        List.collect explode samples
                    |> chunksOf 60
                    |> Seq.map Seq.sum
                    |> Seq.mapi (fun i s -> Dated.mkDated s (startOfDay.AddHours(float i)))
                    |> Seq.toList

        
            


