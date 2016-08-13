namespace BucklingSprings.Aware.Windows

open System
open System.Windows
open System.Windows.Input
open System.Windows.Controls

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Core.Experiments

type ExperimentInProgressWindowViewModel(close, confirmAbandon) =
    let relaunch () =
        let executablePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        let fileName, args = "BucklingSprings.Aware.exe", ""

        let exe = if Environment.currentEnvironment = Environment.Development then
                    System.IO.Path.Combine(executablePath, "..\\..\\..\\Aware\\bin\\Debug\\", fileName)
                    else
                    System.IO.Path.Combine(executablePath, fileName)

        use prc = new System.Diagnostics.Process()
        prc.StartInfo <- System.Diagnostics.ProcessStartInfo(exe)
        prc.StartInfo.Arguments <- args
        prc.Start() |> ignore
        ()
    let abandon () =
        if confirmAbandon() then
            Experiments.abandonRunningOnDate (DateTimeOffset.Now)
            relaunch ()
            close()
        
            
    member x.CloseCommand = AlwaysExecutableCommand(close)
    member x.LaunchHelp = AlwaysExecutableCommand(fun _ -> System.Diagnostics.Process.Start("http://www.aware.am/") |> ignore)
    member x.AbandonExperimentCommand = AlwaysExecutableCommand(abandon)


type ExperimentInProgressWindow() as window =
    inherit Window()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ExperimentInProgressWindow.xaml", UriKind.Relative)) :?> UserControl
    let confirmAbandon () =
       System.Windows.MessageBox.Show("Abandon the experiment in progress? This action cannot be undone.", "Abandon Experiment!", MessageBoxButton.YesNoCancel,  MessageBoxImage.Warning) = MessageBoxResult.Yes
    do
        window.Content <- content
        window.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        window.SizeToContent <- SizeToContent.WidthAndHeight
        window.DataContext <- ExperimentInProgressWindowViewModel(window.Close, confirmAbandon)

