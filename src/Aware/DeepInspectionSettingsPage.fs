namespace BucklingSprings.Aware.Settings

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.ComponentModel

open BucklingSprings.Aware.Core



type DeepInspectionSettingsViewModel() as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let initialConfig = DeepInspectionConfigStore.deepInspectionCurrentConfig()
    let mutable enabled = DeepInspectionConfigStore.isEnabled()
    let mutable screenCaptureEnabled = false
    let mutable archiveLocation = ""
    let mutable minSpace = ""
    let isValidPath (path : string) =
        try
            if Directory.Exists path then
                true
            else
                Directory.CreateDirectory(path) |> ignore
                true
        with
            | _ -> false
    let tryParse (v : string) : option<int> =
                    if String.IsNullOrWhiteSpace(v) then
                        None
                    else
                        let result = ref 0
                        if System.Int32.TryParse(v, result) then
                            Some (!result)
                        else
                            None
    do
        screenCaptureEnabled <- initialConfig.screenCaptureEnabled
        archiveLocation <- initialConfig.root
        minSpace <- string initialConfig.minSpaceInGig
        

    let storeConfig () =
        if enabled then
            let mutable newCfg = DeepInspectionConfigStore.deepInspectionCurrentConfig()
            newCfg <- { newCfg with screenCaptureEnabled = screenCaptureEnabled }
            if isValidPath archiveLocation then
                newCfg <- { newCfg with root = archiveLocation }
            let sizeInGb = tryParse minSpace
            if Option.isSome sizeInGb then
                newCfg <- { newCfg with minSpaceInGig = (Option.get sizeInGb) }
            
            DeepInspectionConfigStore.enableDeepInspection newCfg
        else
            DeepInspectionConfigStore.disableDeepInspection()
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("DeepInspectionDetailsVisibility")) 
        ()
    member x.DeepInspectionEnabled
        with get () = enabled
        and set (value : bool) =
                        enabled <- value
                        storeConfig ()
    member x.CollectScreenImages
        with get () = screenCaptureEnabled
        and set (value : bool) =
                    screenCaptureEnabled <- value
                    storeConfig ()
    member x.ArchiveLocation
        with get () = archiveLocation
        and set (value : string) =
            archiveLocation <- value
            storeConfig ()
    member x.MinSpaceInGb
        with get () = minSpace
        and set (value : string) =
            minSpace <- value
            storeConfig ()
    member x.DeepInspectionDetailsVisibility
        with get () = if enabled then Visibility.Visible else Visibility.Hidden
    member x.MainTitle = "Replay"
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)


type DeepInspectionSettingsPage() as pg =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/DeepInspectionSettingsPage.xaml", UriKind.Relative)) :?> UserControl
    do
        pg.Content <- content
        pg.DataContext <- DeepInspectionSettingsViewModel()
        