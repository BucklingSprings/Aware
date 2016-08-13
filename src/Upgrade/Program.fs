namespace BucklingSprings.Aware.Upgrade

open System
open System.Windows

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics

open Nessos

module Program = 

    type UpgraderArguments =
        | Target of string
        | NoLaunch
        | DisplayVersion of string

    with 
        interface UnionArgParser.IArgParserTemplate with
            member x.Usage =
                match x with
                | UpgraderArguments.Target _ -> "Target to launch after upgrade. Aware or Collector"
                | UpgraderArguments.NoLaunch -> "Do not actually launch the target (Upgrade and exit)"
                | UpgraderArguments.DisplayVersion _ -> "The version to display."

               
    let parseTarget (s : string) =
        let l = if s = null then String.Empty else s.Trim().ToUpperInvariant()
        if l = "COLLECTOR" then
            [LaunchType.Collector]
        elif l = "AWARE" then
            [LaunchType.Aware]
        elif l = "BOTH" then
            [LaunchType.Aware; LaunchType.Collector]
        else
            [LaunchType.Aware]

    let storedVersion = "VER: 1.0.0.0"

    [<STAThread()>]
    [<EntryPoint>]
    let main argv = 
        Diagnostics.initialize EventLogSource.Upgrade
        let parser = UnionArgParser.UnionArgParser.Create<UpgraderArguments>()
        try
            let results = parser.ParseCommandLine(argv)
            let target =  defaultArg (results.TryPostProcessResult(<@ Target @>, parseTarget)) ([LaunchType.Aware; LaunchType.Collector])
            let noLaunch = results.Contains(<@ NoLaunch @>)
            let version = defaultArg (results.TryGetResult(<@ DisplayVersion @>)) storedVersion

            let mainWindow = WelcomeWindow(target, noLaunch, version)
            let app = Application()
            app.Run(mainWindow) |> ignore
            0
        with
            | e -> 
                    Diagnostics.writeToLog EventLogger.Upgrade EntryLevel.Error "%s" (e.ToString())
                    -1


    