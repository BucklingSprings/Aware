namespace BucklingSprings.Aware.Windows.Pages

open BucklingSprings.Aware.Input

open System
open System.Windows
open System.Windows.Input
open System.Windows.Controls
open System.Collections.ObjectModel

type SettingsMenuItem(name : string, lazyPage : Lazy<Page>, frame : Frame) =
    member s.Name = name
    member x.Navigate = AlwaysExecutableCommand(fun _ -> frame.Navigate(lazyPage.Value) |> ignore)

type WidgetHost() as host =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/WidgetHost.xaml", UriKind.Relative)) :?> UserControl
    let widgets = content.FindName("Widgets") :?> Panel
    do
        host.Content <- content

    member host.Add c = ignore(widgets.Children.Add c)


type SettingsHost() as host =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/SettingsHost.xaml", UriKind.Relative)) :?> UserControl
    let settingsFrame = content.FindName("SettingsFrame") :?> Frame
    let menuItems = ObservableCollection<SettingsMenuItem>()
    do
        host.Content <- content
        content.DataContext <- host
        host.Loaded.Add(
            fun _ -> 
                    if menuItems.Count > 0 then
                        let c = menuItems.Item(0).Navigate :> ICommand
                        c.Execute null
                    else
                        ())

    member host.AddSettings name lazyPage= 
        menuItems.Add(SettingsMenuItem(name, lazyPage, settingsFrame))
    member host.MenuItems = menuItems
    member host.Version = 
        let v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
        sprintf "Version: %s" v
