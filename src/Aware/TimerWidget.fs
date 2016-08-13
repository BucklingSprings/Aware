namespace BucklingSprings.Aware.Widgets

open System
open System.Windows


open BucklingSprings.Aware
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Common.Focus
open BucklingSprings.Aware.Core.Goals
open BucklingSprings.Aware.Common.UserConfiguration

[<AllowNullLiteral()>]
type OngoingFocusSessionViewModel(fs : OnGoingFocusSession, color : AssignedBrushes, className : string) =

    member x.FocussedOnDescription = className
    member x.StartTime = Humanize.time fs.current.startTime
    member x.Hours = let ts = (fs.current.endTime - fs.current.startTime) in ts.Hours
    member x.Minutes = let ts = (fs.current.endTime - fs.current.startTime) in ts.Minutes
    member x.Words = System.String.Format("{0:n0}", fs.current.words)
    member x.GoalMinutes = fs.targetLengthInMinutes
    member x.ProgressMinutes = let ts = (fs.current.endTime - fs.current.startTime) in ts.TotalMinutes
    member x.GoalTime = Humanize.minutesFromStartOfDay fs.targetLengthInMinutes
    member x.ClassColor = color.back

[<AllowNullLiteral()>]
type GoalViewModel(h : int, m : int, w : int, g : int, r : int, l : WillReachGoal) =
    let reached = w >= g
    member x.Hours = h
    member x.Minutes = m
    member x.Words = w
    member x.Goal = g
    member x.CanReach = 
        if reached then
            "Reached!"
        else
            match l with
                | WillReachGoal.Likely -> sprintf "Likely (%d%% Chance)" r
                | WillReachGoal.UnLikely -> sprintf "Unikely (%d%% Chance)" r

   

type TimerViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<(OnGoingFocusSession * AssignedBrushes * MoreOf * string) option * DailyGoalStatus option>(wds, true)

    let mutable ongogingFocusSession = null
    let mutable goal : GoalViewModel = null


    let readData (wd : WorkingData) = 
        async {
            let ongoing = wd.focusSessions.Value |> snd
            let goalDay = DataDateRangeFilterUtils.endDt wd.configuration.dateRangeFilter
            let goalStatus = GoalCalculator.goalStatus wd.todaysPerformance wd.historicalPerformance goalDay
            if Option.isSome ongoing then
                let fs : OnGoingFocusSession = Option.get ongoing
                return Some (
                                fs, 
                                wd.configuration.classification.colorMap  fs.current.sessionClassId,
                                wd.configuration.classification.moreOfMap  fs.current.sessionClassId,
                                wd.configuration.classification.classNames fs.current.sessionClassId), goalStatus
            else
                return None, goalStatus
        }
    let showData sessionAndColor dailyGoalStatus =
        let fps = LazyServices.focusSessionProgressReportingService.Value
        let mutable progressPct = 0.0
        if Option.isSome sessionAndColor then
            let fs, color, moreOf, name = Option.get sessionAndColor
            if fs.current.idle then
                fps.DisplayProgressState FocusSessionProgress.NoOngoingSession dailyGoalStatus
                Broadcast.broadcast Broadcast.Idle dailyGoalStatus
            elif fs.stalled then
                fps.DisplayProgressState FocusSessionProgress.Stalled dailyGoalStatus
                Broadcast.broadcast (Broadcast.Stalled name) dailyGoalStatus
            else
                match moreOf with
                    | MoreOf.MoreOf -> fps.DisplayProgressState FocusSessionProgress.RunningMoreOf dailyGoalStatus
                    | MoreOf.LessOf -> fps.DisplayProgressState FocusSessionProgress.RunningLessOf dailyGoalStatus
                    | MoreOf.Neutral -> fps.DisplayProgressState FocusSessionProgress.RunningNeutral dailyGoalStatus

                let mins = let ts = (fs.current.endTime - fs.current.startTime) in ts.TotalMinutes
                let target = float fs.targetLengthInMinutes
                progressPct <- (mins / target)
                ongogingFocusSession <- OngoingFocusSessionViewModel(fs, color, name)
                let p = {
                    Broadcast.Progress.taskName = name
                    Broadcast.Progress.percentageDone = (int ((mins / target) * 100.0))
                    Broadcast.Progress.moreOrLess = moreOf
                    Broadcast.Progress.minutes = int mins
                    Broadcast.Progress.wordCount = fs.current.words
                }
                Broadcast.broadcast (Broadcast.Working p) dailyGoalStatus
        else
            fps.DisplayProgressState FocusSessionProgress.NoOngoingSession dailyGoalStatus
            ongogingFocusSession <- null
            Broadcast.broadcast Broadcast.Idle dailyGoalStatus

        if Option.isSome dailyGoalStatus then
            let g : DailyGoalStatus = Option.get dailyGoalStatus
            let ts = TimeSpan.FromMinutes (float g.minutesToday)
            goal <- GoalViewModel(ts.Hours,ts.Minutes, g.wordsToday, g.wordGoal, g.canReach, g.likelyhood)
            progressPct <- (float g.wordsToday) / (float g.wordGoal)

        fps.ProgressPercentage <- progressPct
        
        vm.TriggerPropertyChanged("SubTitle")
        vm.TriggerPropertyChanged("OngoingFocusSession")
        vm.TriggerPropertyChanged("Goal")

    member x.OngoingFocusSession = ongogingFocusSession
    member x.Goal = goal
    override x.ReadData _ wd = readData wd
    override x.ShowData d = 
        let focus,goalStatus = d
        showData focus goalStatus
    override x.Title = "Goals & Timer"
    override x.SubTitle = if ongogingFocusSession = null then "--" else sprintf "START: %s" (ongogingFocusSession.StartTime)



type TimerWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<TimerViewModel, (OnGoingFocusSession * AssignedBrushes * MoreOf * string) option * DailyGoalStatus option>(wds, "TimerWidgetElement.xaml")
    override x.CreateViewModel wds = TimerViewModel(wds)

module TimerWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast TimerWidgetElement(wds)