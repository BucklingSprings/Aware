namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.ActivitySamples



module DailyTotalsExporter =

    let exportToFile startDate endDate fileName =
        let toMins s =
            let ts = TimeSpan.FromSeconds(float s)
            string (int ts.TotalMinutes)
        let toWords k =
            let words = TypedActivitySamples.keyStrokesToWords k
            string words
        let count = ref 0
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Date,Minutes,Words")
        let totals = DailyTotalsStore.dailyTotalsForDateRage startDate endDate
        totals
            |> List.iter (fun t ->
                                    let row = sprintf "%s,%s,%s" (Export.formatDateOnly' t.sampleDate) (toMins t.seconds) (toWords t.keyboardActivity)
                                    writer.WriteLine row)
        List.length totals