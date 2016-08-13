namespace BucklingSprings.Aware.Exporter

open System

open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

open Nessos

module Program =

    type ExportType =
        | Samples
        | Words
        | StartEndTimes
        | DailyTotals
        | HourlyTotals
        | ExperimentList

    type ExporterArguments =
        | Start_Date of string
        | End_Date of string
        | Output_File_Name of string
        | ExportType of string
        | Sample_Filter_Text of string
        | Include_Raw
    with 
        interface UnionArgParser.IArgParserTemplate with
            member x.Usage =
                match x with
                | ExporterArguments.Start_Date _ -> "Export data starting on this day (inclusive)."
                | ExporterArguments.End_Date _ -> "Export data from start Date to this day."
                | ExporterArguments.Output_File_Name _ -> "File name to which the data is written."
                | ExporterArguments.ExportType _ -> "What should be exported? Samples, Words, StartEndTimes, HourlyTotal, DailyTotals, ExperimentList"
                | ExporterArguments.Include_Raw -> "Include raw data if applicable."
                | ExporterArguments.Sample_Filter_Text  _ -> "Only include samples that contain this text."

    let parseDate p =
        let d = ref DateTimeOffset.Now
        if DateTimeOffset.TryParse(p, d) then
            !d
        else
            failwith (sprintf "Invalid date '%s'." p)

    let parseExportType (t : string) =
        if String.IsNullOrWhiteSpace(t) then
            ExportType.Samples
        else
            match t.ToUpperInvariant().Trim() with
                | "WORDS" -> ExportType.Words
                | "SAMPLES" -> ExportType.Samples
                | "STARTENDTIMES" -> ExportType.StartEndTimes
                | "DAILYTOTALS" -> ExportType.DailyTotals
                | "EXPERIMENTLIST" -> ExportType.ExperimentList
                | "HOURLYTOTALS" -> ExportType.HourlyTotals
                | _ -> ExportType.Samples

    [<EntryPoint>]
    let main argv = 
        let parser = UnionArgParser.UnionArgParser.Create<ExporterArguments>()
        try
            let results = parser.ParseCommandLine(argv)
            let yesterday = DateTimeOffset.Now.AddDays(-1.0).StartOfDay
            let startDt =  defaultArg (results.TryPostProcessResult(<@ Start_Date @>, parseDate)) yesterday
            let endDt = defaultArg (results.TryPostProcessResult(<@ End_Date @>, parseDate)) (startDt)
            let fileName = defaultArg (results.TryGetResult(<@ Output_File_Name @>)) "output.csv"
            let exportType = defaultArg (results.TryPostProcessResult(<@ ExportType @>, parseExportType))  ExportType.Samples
            let includeRaw = results.Contains(<@ Include_Raw @>)
            let sampleFilter = defaultArg (results.TryGetResult(<@ Sample_Filter_Text @>)) ""
            
            let exporter = match exportType with
                            | ExportType.Samples -> ClassLevelExporter.exportToFile sampleFilter
                            | ExportType.Words -> WordExporter.exportToFile
                            | ExportType.StartEndTimes -> StartEndTimesExporter.exportToFile includeRaw
                            | ExportType.DailyTotals -> DailyTotalsExporter.exportToFile
                            | ExportType.HourlyTotals -> HourlyTotalsExporter.exportToFile
                            | ExportType.ExperimentList -> ExperimentListExporter.exportToFile

            let count = exporter startDt.StartOfDay endDt.EndOfDay fileName
            let formatDate (d : DateTimeOffset) = d.Date.ToShortDateString()
            printfn "Exporting data from %s to %s." (formatDate startDt) (formatDate endDt)
            printfn "%i records written to %s" count fileName
            count
        with
            | a -> 
                Console.Error.WriteLine a.Message
                -1
    
    