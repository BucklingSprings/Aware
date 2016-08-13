namespace BucklingSprings.Aware.Settings

open System
open System.Windows
open System.Windows.Controls
open System.ComponentModel

open BucklingSprings.Aware
open BucklingSprings.Aware.Core.Goals
open BucklingSprings.Aware.Input

type GoalSettingsViewModel(dt : DateTimeOffset, automaticGoal : int) as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let initialConfig = GoalConfigStore.goalConfig dt
    let mutable enabled = initialConfig.enabled
    let mutable automaticGoalsEnabled = initialConfig.automaticGoalSetting
    let mutable manualGoal = initialConfig.wordGoal
    let mutable goalForToday = initialConfig.wordsToday
    let helpCommand = AlwaysExecutableCommand(fun _ -> System.Diagnostics.Process.Start("http://www.aware.am/help/settings/goals") |> ignore)
    let storeGoalConfig () =
        let cfg =  {
                enabled = enabled
                automaticGoalSetting = automaticGoalsEnabled
                wordGoal = if automaticGoalsEnabled then 0 else manualGoal
                wordsToday = goalForToday
            }
        GoalConfigStore.storeGoalConfig dt cfg
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("CanEditGoal")) 
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("GoalWords")) 
        ()
    let tryParse (v : string) : option<int> =
                    if String.IsNullOrWhiteSpace(v) then
                        None
                    else
                        let result = ref 0
                        if System.Int32.TryParse(v, result) then
                            Some (!result)
                        else
                            None
    member x.MainTitle = "Goals"
    member x.GoalsEnabled
        with get() = enabled
        and set (v : bool) =
            enabled <- v
            storeGoalConfig()
    member x.AutomaticGoalSettingEnabled
        with get() = automaticGoalsEnabled
        and set (v : bool) =
            automaticGoalsEnabled <- v
            storeGoalConfig()

    member x.CanEditGoal = automaticGoalsEnabled = false
    member x.GoalWords
        with get() =
            let words = if automaticGoalsEnabled then automaticGoal else manualGoal
            if words > 0 then
                string words
            else
                ""
        and set (v : string) = 
            let g = tryParse v
            if Option.isSome g then
                manualGoal <- Option.get g
            else
                manualGoal <- 0
            storeGoalConfig()
    member x.GoalForToday
        with get() =
            if goalForToday > 0 then
                string goalForToday
            else
                ""
        and set (v : string) =
            let g = tryParse v
            if Option.isSome g then
                goalForToday <- Option.get g
            else
                goalForToday <- 0
            storeGoalConfig()
    member x.HelpCommand = helpCommand
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)


type GoalSettingsPage(wds : WorkingDataService) as pg =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/GoalSettingsPage.xaml", UriKind.Relative)) :?> UserControl
    do
        pg.Content <- content
        pg.DataContext <- GoalSettingsViewModel(DateTimeOffset.Now, GoalCalculator.automaticGoal wds.WorkingData.historicalPerformance)
        pg.Unloaded.Add(fun _ -> Async.Start(wds.Refresh()))