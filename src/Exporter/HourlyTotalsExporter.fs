namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.ActivitySamples



module HourlyTotalsExporter =

    let exportToFile startDate endDate fileName =
        let toMins s =
            let ts = TimeSpan.FromSeconds(float s)
            string (int ts.TotalMinutes)
        let toWords k =
            let words = TypedActivitySamples.keyStrokesToWords k
            string words
        let count = ref 0
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Date,Hour,Minutes,Words")
        let count = ref 0
        let writeRow (t : HourlyTotal) =
            let row = sprintf "%s,%d,%s,%s" (Export.formatDateOnly' t.sampleDate) t.sampleHour (toMins t.seconds) (toWords t.keyboardActivity)
            writer.WriteLine row
            incr count

        let totals = HourlyTotalsStore.totalsForDateRage startDate endDate writeRow
        
        !count