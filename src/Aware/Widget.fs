namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Threading

open System
open System.Windows
open System.Windows.Controls
open System.ComponentModel

type Widget = WorkingDataService -> UIElement

[<AbstractClass>]
type WidgetViewModelBase<'a>(wds : WorkingDataService, refreshable : bool) as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let showHelp () = System.Diagnostics.Process.Start("http://www.aware.am/help/") |> ignore
    let showReplay () = if Option.isNone vm.ReplayDate then
                            ()
                        else
                            Replay.show (Option.get vm.ReplayDate) (wds.ConfigurationService.CurrentConfiguration)
    let populateAllData (initial) = 
        async {
            do! Async.SwitchToThreadPool()
            let wd = wds.WorkingData
            let! d = vm.ReadData initial wd
            do! Async.SwitchToContext (UIContext.instance())
            vm.ShowData d
            vm.TriggerPropertyChanged("LastRefreshed")
        }
    do
        if refreshable then
            wds.WorkingDataRefreshed.Add(fun _ -> Async.Start(populateAllData(false)))
        else
            wds.WorkingDataChanged.Add(fun _ -> Async.Start(populateAllData(false)))
        

    member x.InitialLoad () =
        populateAllData(true)
    
        
    member x.TriggerPropertyChanged nm = propertyChanged.Trigger(vm, PropertyChangedEventArgs(nm))
    member x.LastRefreshed
        with get () : string = 
            let n = System.DateTimeOffset.Now
            n.ToString()

    abstract ReadData : bool -> WorkingData -> Async<'a>
    abstract ShowData : 'a -> Unit
    abstract Title : string
    abstract SubTitle : string
    abstract ReplayDate : Option<DateTimeOffset>
    default x.SubTitle = System.String.Empty
    default x.ReplayDate = None
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    member this.ShowHelp = AlwaysExecutableCommand(showHelp)
    member this.Replay = AlwaysExecutableCommand(showReplay)
    member this.ReplayVisibility = if  DeepInspectionConfigStore.isEnabled() && Option.isSome this.ReplayDate then Visibility.Visible else Visibility.Collapsed
    interface INotifyPropertyChanged with
       member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
       member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

[<AbstractClass()>]
type StandardWidgetElementBase<'v, 'd when 'v :> WidgetViewModelBase<'d>>(wds : WorkingDataService, xamlSource) as uc =
    inherit UserControl()

    let uri = sprintf "/BucklingSprings.Aware;component/%s" xamlSource
    let mutable viewModel : option<'v> = None


    let widget = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/WidgetElementBase.xaml", UriKind.Relative)) :?> UserControl
    let widgetBody = Application.LoadComponent(Uri(uri, UriKind.Relative)) :?> UserControl
    let widgetBodyContainer = widget.FindName("WidgetBodyContainer") :?> ContentControl

    let init () =
        async {
            trace "Creating VM %s" xamlSource
            do! Async.SwitchToContext (UIContext.instance())
            uc.InitialLoad(widgetBody)
            let vm = uc.CreateViewModel(wds)
            viewModel <- Some vm
            do! Async.SwitchToThreadPool()
            do! vm.InitialLoad()
            do! Async.SwitchToContext (UIContext.instance())
            widget.LayoutTransform <- System.Windows.Media.ScaleTransform(1.0,1.0)
            uc.Content <- widget
            widgetBodyContainer.Content <- widgetBody
            widget.DataContext <- vm
            return ()
        }
    
    do
        uc.Initialized.Add(fun _ -> Async.Start(init()))

    member x.ViewModel = viewModel
    abstract CreateViewModel : WorkingDataService -> 'v
    abstract InitialLoad : UserControl -> Unit
    default x.InitialLoad _ = ()
    