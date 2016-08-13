namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Classifiers
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

open BucklingSprings.Aware.Store

module StartEndTimesExporter =
    
    let exportToFile includeRaw (startDate : DateTimeOffset) (endDate : DateTimeOffset) fileName =
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Date,Start,RawStart,End,RawEnd,StartMinutes,EndMinutes")

        let ss = SummaryStore.allSummaries()
        let filter = List.filter (fun (s : StoredSummary) -> s.summaryTimeAndDate >= startDate.StartOfDay && s.summaryTimeAndDate < endDate.StartOfDay)
        
        let minAndMax (ss: ActivitySample list) =
            let s = (ss |> List.minBy (fun s -> s.sampleStartTimeAndDate)).sampleStartTimeAndDate
            let e = (ss |> List.maxBy (fun s -> s.sampleEndTimeAndDate)).sampleEndTimeAndDate
            (s,e)

        let rawData = 
            if includeRaw then
                ActivitySamplesStore.processAllInDateRange startDate endDate Seq.toList
                    |> Seq.filter (fun s -> s.inputActivity.IsNotIdle)
                    |> Seq.groupBy (fun s -> s.sampleStartTimeAndDate.StartOfDay)
                    |> Seq.map (fun (d,ss) -> (Export.formatDateOnly d, ss |> Seq.toList |> minAndMax))
                    |> Map.ofSeq
            else
                Map.ofSeq([])

        let summaries = LazyTypedStoredSummariesReader.read (filter ss)
        let dayLengths = summaries.Value.dayLengths

        let writeRow (d : WithDate<DayStartAndEndTimes.DayLength>) =
            let dt, dayLength = d
            let day = Export.formatDateOnly dt
            let startMin = StartAndEndTime.toMinutesFromStartOfDay dayLength.startTime
            let endMin = StartAndEndTime.toMinutesFromStartOfDay dayLength.endTime
            let startTime = Humanize.minutesFromStartOfDayAsTime startMin
            let endTime = Humanize.minutesFromStartOfDayAsTime endMin
            let rawStart, rawEnd = if rawData.ContainsKey(day) then
                                        let s,e = rawData.Item(day)
                                        let fmt s = s
                                                    |> StartAndEndTime.fromDtTime
                                                    |> StartAndEndTime.toMinutesFromStartOfDay
                                                    |> Humanize.minutesFromStartOfDayAsTime
                                        fmt s, fmt e
                                   else
                                    "na", "na"
                    
            let row = sprintf "%s,%s,%s,%s,%s,%d,%d" day startTime rawStart endTime rawEnd startMin endMin  
            writer.WriteLine row

        List.iter writeRow dayLengths
       
        List.length dayLengths
