namespace BucklingSprings.Aware.Windows

open System
open System.Windows
open System.Windows.Controls

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Input


type UpdateWindowViewModel(update : Updates.UpgradeInformation, onCancel) =
    let launch (s : string)=
        fun () -> System.Diagnostics.Process.Start(s) |> ignore

    member x.NewVersion = update.Version
    member x.Download = AlwaysExecutableCommand(launch update.DownloadUrl)
    member x.CancelUpdate = AlwaysExecutableCommand(onCancel)
    

type UpdateWindow(update : Updates.UpgradeInformation, owner : Window) as window =
    inherit Window()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/UpdateWindow.xaml", UriKind.Relative)) :?> UserControl
    do
        window.Owner <- owner
        window.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        window.SizeToContent <- SizeToContent.WidthAndHeight
        window.WindowStyle <- WindowStyle.None
        window.ResizeMode <- ResizeMode.NoResize
        window.AllowsTransparency <- true
        window.Content <- content
        window.Padding <- Thickness(0.0)
        window.BorderThickness <- Thickness(0.0)
        content.DataContext <- new UpdateWindowViewModel(update, fun _ -> window.Close())
