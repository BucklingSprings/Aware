namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.Classifiers

module WordExporter =
    
    let exportToFile startDate endDate fileName =
        let count = ref 0
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Word,Count")
        let p (s : ActivitySample) =
            Phrase.extractWords s.activityWindowDetail
            
        let samples = ActivitySamplesStore.processAllInDateRange startDate endDate Seq.toList
        let words = Seq.collect p samples
        let counts = Seq.countBy id words
        counts |> Seq.iter (fun (w, c) -> 
                                    let row = sprintf "\"%s\",%d" (Export.escape w) c
                                    incr count
                                    writer.WriteLine row
                                )
        !count

