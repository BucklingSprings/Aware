namespace BucklingSprings.Aware

open System
open System.Windows
open System.Windows.Navigation


open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Experiments

module Program =

    [<EntryPoint()>]
    [<STAThread()>]
    let main args =
        
        let splash = SplashScreen("AwareSplash.jpg")
        splash.Show(true)

        Diagnostics.initialize EventLogSource.Aware
        Diagnostics.writeToLog EventLogger.Aware EntryLevel.Information "%s - %A" "Aware Start Up" (DateTimeOffset.Now)

        AppDomain.CurrentDomain.UnhandledException.Add(fun e ->
                Diagnostics.writeToLog EventLogger.Aware EntryLevel.Error "%s" (e.ExceptionObject.ToString())
            )
        
        let mainWindow = if Experiments.canStartAware (DateTimeOffset.Now) then
                            Windows.DashboardWindow() :> Window
                         else
                            Windows.ExperimentInProgressWindow() :> Window
                            
        let app = Application()
        app.Run(mainWindow) |> ignore
        0
