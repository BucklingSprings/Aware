namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store

module ClassLevelExporter =

    let exportToFile sampleFilter startDate endDate fileName =
        let count = ref 0
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Text,Class,Classifier,Start,End,DurationInSeconds")
        let p (s : ActivitySample) =
            s.classes
                |> Seq.iter (fun c ->
                                let text = s.activityWindowDetail.windowText
                                let className = c.assignedClass.className
                                let classifierName = c.assignedClass.classifier.classifierName
                                let startDate = Export.formatDate s.sampleStartTimeAndDate
                                let endDate = Export.formatDate s.sampleStartTimeAndDate
                                let seconds = s.sampleEndTimeAndDate.Subtract(s.sampleStartTimeAndDate).TotalSeconds |> int |> string
                                let row = sprintf "\"%s\", \"%s\", \"%s\", %s, %s, %s" (Export.escape text) className classifierName startDate endDate seconds
                                let includeRow = String.IsNullOrWhiteSpace(sampleFilter) || text.ToUpperInvariant().Contains(sampleFilter.ToUpperInvariant())
                                if includeRow then
                                    incr count;
                                    writer.WriteLine row
                                else
                                    ())
        ActivitySamplesStore.processAllInDateRange startDate endDate (Seq.iter p)
        !count
