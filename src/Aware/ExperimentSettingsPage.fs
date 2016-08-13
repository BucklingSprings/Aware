namespace BucklingSprings.Aware.Settings

open System
open System.Windows
open System.Windows.Controls
open System.ComponentModel
open System.Collections.ObjectModel


open BucklingSprings.Aware
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Experiments

type Behaviour(name : string, kind : string) =
    member x.Name = name
    member x.Kind = kind

type CurrentExperimentViewModel(e : ExperimentDetails option, confirmAbandon, onAbandon) =
    let now = DateTimeOffset.Now
    let extract f = if Option.isNone e then "" else f (Option.get e)
    let abandon () = if confirmAbandon() then
                        Experiments.abandonRunningOnDate now
                        onAbandon()
    member x.Name = extract (fun ed -> ed.name)
    member x.StartedOn = extract (fun ed -> Humanize.dateAndDay ed.startDate)
    member x.EndsOn = extract (fun ed -> let s = ed.startDate in Humanize.dateAndDay  (s.AddDays(float ed.durationInDays)))
    member x.Behaviour = extract (fun ed -> Experiments.toDescription ed.kind)
    member x.TodaysOutcome = extract (fun ed -> 
                                                    if (Experiments.runningOnDate now ed) then
                                                        if (Experiments.outcomeForDay now ed) then "Heads" else "Tails"
                                                    else
                                                        "NA")
    member x.AbandonCommand = AlwaysExecutableCommand(abandon)


type NewExperimentViewModel(started) =
    let b k = Behaviour(Experiments.toDescription k, Experiments.toShortDescription k)
    let behaviours = ObservableCollection<Behaviour>([ b ExperimentKind.OnOff; b ExperimentKind.On; b ExperimentKind.Off ])
    let parseDuration s =
        let i = ref 0
        if Int32.TryParse(s, i) then
            !i
        else
            0
    let mutable name = String.Empty
    let mutable duration = 90
    let mutable kind = Experiments.toShortDescription ExperimentKind.OnOff
    let startNew () =
        let e = {
            ExperimentDetails.name = name
            ExperimentDetails.durationInDays = duration
            ExperimentDetails.kind = Option.get( Experiments.parseKind kind)
            ExperimentDetails.startDate = DateTimeOffset.Now
        }
        if Experiments.startNewExperiment e then
            started()
    let startNewCommand = DelegatingCommand(startNew)
    let enableDisableStartNew () =
        if String.IsNullOrWhiteSpace(name) || duration < 1 || String.IsNullOrWhiteSpace(kind) then
            startNewCommand.ChangeCanExecuteTo false
        else
            startNewCommand.ChangeCanExecuteTo true
    member x.Duration
        with get() = string duration
        and set (value) = 
            duration <- parseDuration value
            enableDisableStartNew()
    member x.Name
        with get() = name
        and set (value) =
            name <- Text.RegularExpressions.Regex.Replace(value, "[^a-zA-Z0-9]", "")
            enableDisableStartNew()
    member x.Kind
        with get() = kind
        and set (value : string) =
            enableDisableStartNew()
    member x.Behaviours = behaviours
    member x.StartNewCommand = startNewCommand

type ExperimentSettingsViewModel() as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let helpCommand = AlwaysExecutableCommand(fun _ -> System.Diagnostics.Process.Start("http://www.aware.am/help/settings/experiments") |> ignore)
    
    
    let mutable currentExperimentDetails = Experiments.experimentInProgressOn DateTimeOffset.Now
    let confirm () =
        System.Windows.MessageBox.Show("Abandon the experiment in progress? This action cannot be undone.", "Abandon Experiment!", MessageBoxButton.YesNoCancel,  MessageBoxImage.Warning) = MessageBoxResult.Yes
    
    let onAbandonOrStart() =
        currentExperimentDetails <- Experiments.experimentInProgressOn DateTimeOffset.Now
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("NewExperimentVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("CurrentExperimentVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("CurrentExperiment"))    
    let newExperiment = NewExperimentViewModel(onAbandonOrStart)
    
    member x.HelpCommand = helpCommand
    member x.NewExperiment = newExperiment
    member x.CurrentExperiment = CurrentExperimentViewModel(currentExperimentDetails, confirm, onAbandonOrStart)
    member x.NewExperimentVisibility = if Option.isNone currentExperimentDetails then Visibility.Visible else Visibility.Collapsed
    member x.CurrentExperimentVisibility = if Option.isSome currentExperimentDetails then Visibility.Visible else Visibility.Collapsed
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)


type ExperimentSettingsPage() as pg =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ExperimentSettingsPage.xaml", UriKind.Relative)) :?> UserControl
    do
        pg.Content <- content
        pg.DataContext <- ExperimentSettingsViewModel()