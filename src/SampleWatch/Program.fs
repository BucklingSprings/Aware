namespace BucklingSprings.Aware.SampleWatch

open System
open System.Windows

open BucklingSprings.Aware.Store

module Program =

    [<EntryPoint()>]
    [<STAThread()>]
    let main args =
        
        Store.initialize(false)

        let mainWindow = SampleWatchMainWindow()
        let app = Application()
        app.Run(mainWindow) |> ignore
        0 // return an integer exit code
