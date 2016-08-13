namespace BucklingSprings.Aware.Core.Summaries

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.DayStartAndEndTimes

[<RequireQualifiedAccess()>]
[<NoComparison()>]
type ActivitySummary = 
    {
        wordCount : int
        minuteCount : int
    }
    static member (+) (x : ActivitySummary, y : ActivitySummary) =
        { wordCount = x.wordCount + y.wordCount; minuteCount = x.minuteCount + y.minuteCount }
    static member Zero = { wordCount = 0; minuteCount = 0}

[<RequireQualifiedAccess()>]
[<NoComparison()>]
type ActivitySummaryStatistics = 
    {
        wordStatistics : FiveNumberSummary<float>
        minuteStatistics : FiveNumberSummary<float>
        wordPerMinuteStatistics : FiveNumberSummary<float>
    }
    static member Zero = { ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.zerof; ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.zerof; ActivitySummaryStatistics.wordStatistics = StatisticalSummary.zerof}
    static member SomeNegativeValue = { ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.someNegativeValuef; ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.someNegativeValuef; ActivitySummaryStatistics.wordStatistics = StatisticalSummary.someNegativeValuef}



module Summaries = 

    let maxValues (x : ActivitySummary) (y : ActivitySummary) =
        {
            ActivitySummary.wordCount = max x.wordCount y.wordCount;
            ActivitySummary.minuteCount = max x.minuteCount y.minuteCount;
        }

    let max (xs : ActivitySummary list) = xs |> List.fold maxValues ActivitySummary.Zero

    
    let wordsPerMinute (x : ActivitySummary) = if x.minuteCount = 0 then 0 else x.wordCount / x.minuteCount

    let summarize (xs : ActivitySummary list) : ActivitySummaryStatistics =
        let words = xs |> List.map (fun s -> s.wordCount)
        let minutes = xs |> List.map (fun s -> s.minuteCount)
        let wpms =  xs |> List.map wordsPerMinute
        {
            ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.summarize minutes;
            ActivitySummaryStatistics.wordStatistics = StatisticalSummary.summarize words;
            ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.summarize wpms
        }

    let medians (s : ActivitySummaryStatistics) : ActivitySummary =
        {
            ActivitySummary.minuteCount = s.minuteStatistics.median |> (ceil >> int);
            ActivitySummary.wordCount = s.wordStatistics.median |> (ceil >> int);
        }

    let maxFromStats (s : ActivitySummaryStatistics) : ActivitySummary =
        {
            ActivitySummary.minuteCount = s.minuteStatistics.maximum |> (ceil >> int);
            ActivitySummary.wordCount = s.wordStatistics.maximum |> (ceil >> int);
        }

    let wpmMedian (s : ActivitySummaryStatistics) : int = s.wordPerMinuteStatistics.median |> (ceil >> int)
    let wpmMax (s : ActivitySummaryStatistics) : int = s.wordPerMinuteStatistics.maximum |> (ceil >> int)
        


    let average (xs : ActivitySummary list) : ActivitySummary =
        if List.isEmpty xs then
            ActivitySummary.Zero
        else
            let averageWordCount = xs |> Seq.averageBy (fun a -> float a.wordCount) |> round |> int
            let averageMinuteCount = xs |> Seq.averageBy (fun a -> float a.minuteCount) |> round |> int
            {
                ActivitySummary.wordCount = averageWordCount;
                ActivitySummary.minuteCount = averageMinuteCount;
            }

    


