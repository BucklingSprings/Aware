#nowarn "52"

namespace BucklingSprings.Aware.Store.Development.Utlities

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store


module Program = 

    let quote (s : string) = sprintf "\"%s\"" (s.Replace('"', '\''))
    let quotei (i : int) = sprintf "\"%s\"" (string i)
    let line s = sprintf "%s,%s,%s,%s,%s" (quote s.windowTitle) (quote s.processName) (quotei s.inputActivity.keyboardActivity) (quotei s.inputActivity.mouseActivity) (quote (s.timeAndDate.ToString()))

    let dumpAsUnSaved () =
        
        use sw = new System.IO.StreamWriter("C:\\aware_dev\\dump.csv")
        ActivitySamplesStore.processAllAsUnsavedSamplesInOrder
            (fun s ->
                sw.WriteLine (line s)
            )

    let loadSamplesFromDump () =
        use reader = new Microsoft.VisualBasic.FileIO.TextFieldParser("C:\\aware_dev\\dump.csv")
        reader.TextFieldType <- Microsoft.VisualBasic.FileIO.FieldType.Delimited
        reader.Delimiters <- [| "," |]
        reader.HasFieldsEnclosedInQuotes <- true
        let rec processSamples state i c =
            if reader.EndOfData then
                printfn "Count: %d -> %d" i c
            else
                let fields = reader.ReadFields()
                let s = {
                            UnsavedSample.windowTitle = fields.[0]
                            UnsavedSample.processName = fields.[1]
                            UnsavedSample.inputActivity = {keyboardActivity = (int fields.[2]); mouseActivity = (int fields.[3])}
                            UnsavedSample.timeAndDate = System.DateTimeOffset.Parse fields.[4]
                        }
                let (savedNew, state) = SampleStore.save s state
                let c' = if savedNew then c + 1 else c
                processSamples (Some state) (i + 1) c'
        processSamples None 0 0
    ()
        
    [<EntryPoint>]
    let main argv = 
        Store.initialize (false)
        let cmdLineOptions = Set(System.Environment.GetCommandLineArgs())
        if cmdLineOptions.Contains("Dump") then
            dumpAsUnSaved()
        if cmdLineOptions.Contains("LoadDump") then
            loadSamplesFromDump()
        0
