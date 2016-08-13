namespace BucklingSprings.Aware.AddOns.Motivate

open System
open System.IO
open System.Xml

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics

open Nessos.UnionArgParser

module Program =

    type Arguments =
        | Task_Name of string
        | Percentage_Done of int
        | Minutes of int
        | Word_Count of int
        | More_Or_Less of string
        | Goal_Likely of string // Likely - Unlikely
        | Goal_Words of int // What is the goal
        | Goal_Words_Done of int // Number of words towards the goal
        | Goal_Likely_Percent of int // What is the chance the goal will be reached

    with
        interface IArgParserTemplate with
            member s.Usage = 
                match s with
                    | Arguments.Task_Name _ -> "Name of the current task."
                    | Arguments.Percentage_Done _ -> "Percentage done as an integral number petween 0 and 100."
                    | Arguments.Minutes _ -> "Minutes spent on task so far."
                    | Arguments.Word_Count _ -> "Number of words typed so far."
                    | Arguments.More_Or_Less _ -> "More, Less or Neutral. Should you be doing More or Less of this task."
                    | Arguments.Goal_Likely _ -> "Is the goal likely to be met - Likely\Unlikely"
                    | Arguments.Goal_Words _ -> "Goal Words - Can be zero"
                    | Arguments.Goal_Words_Done _ -> "Words typed towards the goal"
                    | Arguments.Goal_Likely_Percent _ -> "What is the chance the goal will be reached - percentage - 0-100"


    type AddOnConfig = {
        Program: string
        WorkingDirectory : string
        Arguments : string}
        

    let allConfigFiles () =
        let root = Environment.defaultAddOnStoreRoot
        if Directory.Exists root then
            Seq.toList ( Directory.EnumerateFiles(root, "*.mbroadcast.xml", SearchOption.TopDirectoryOnly))
        else
            List.empty

    let replaceArgs (results : ArgParseResults<Arguments>) (s : string) =
        let quote s = sprintf "\"%s\"" s
        let replace (o : string) (n : string) (s : string) = s.Replace(o, n)
        let task = quote (results.GetResult(<@ Task_Name @>, "--"))
        s
            |> replace "{TASK_NAME}" task
            |> replace "{PERCENTAGE_DONE}" (string (results.GetResult(<@ Percentage_Done @>, 0)))
            |> replace "{MINUTES}" (string (results.GetResult(<@ Minutes @>, 0)))
            |> replace "{WORD_COUNT}" (string (results.GetResult(<@ Word_Count @>, 0)))
            |> replace "{MORE_OR_LESS}" (results.GetResult(<@ More_Or_Less @>, "Neutral"))
            |> replace "{GOAL_LIKELY}" (results.GetResult(<@ Goal_Likely @>, "Likely"))
            |> replace "{GOAL_WORDS}" (string (results.GetResult(<@ Goal_Words @>, 0)))
            |> replace "{GOAL_WORDS_DONE}" (string (results.GetResult(<@ Goal_Words_Done @>, 0)))
            |> replace "{GOAL_LIKELY_PCT}" (string (results.GetResult(<@ Goal_Likely_Percent @>, 0)))
        

    let parseConfig (configFile : string) = 
        try
            let doc = XmlDocument()
            doc.Load(configFile)
            Some {
                Program = doc.SelectSingleNode("/AwareAddOn/Program").InnerText.Trim()
                WorkingDirectory = doc.SelectSingleNode("/AwareAddOn/WorkingDirectory").InnerText.Trim()
                Arguments = doc.SelectSingleNode("/AwareAddOn/Arguments").InnerText.Trim()
            }
        with
            | a -> 
                Diagnostics.writeToLog EventLogger.MBroadcast EntryLevel.Error "%s" a.Message
                None

    let broadcastTo (results : ArgParseResults<Arguments>) (c : AddOnConfig) =
        let file = Path.Combine(c.WorkingDirectory, c.Program)
        try
            let args = replaceArgs results c.Arguments
            let psi = Diagnostics.ProcessStartInfo(file, args)
            psi.WorkingDirectory <- c.WorkingDirectory
            System.Diagnostics.Process.Start(psi) |> ignore
        with
           | a -> Diagnostics.writeToLog EventLogger.MBroadcast EntryLevel.Error "%s" a.Message
        

    [<EntryPoint>]
    let main argv = 
        Diagnostics.initialize EventLogSource.MBroadcast

        let parser = UnionArgParser.Create<Arguments>()
        try
            let results = parser.ParseCommandLine(argv)
            allConfigFiles ()
                    |> List.choose parseConfig
                    |> List.iter (broadcastTo results)
            0
         with
            | a -> 
                Diagnostics.writeToLog EventLogger.MBroadcast EntryLevel.Error "%s" a.Message
                -1
