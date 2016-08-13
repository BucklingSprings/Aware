namespace BucklingSprings.Aware

open BucklingSprings.Aware.Core.Goals

open System.Windows.Shell

type FocusSessionProgress =
    | NoOngoingSession
    | RunningMoreOf
    | RunningLessOf
    | RunningNeutral
    | Stalled

type FocusSessionProgressReportingService() =
    let tbi = TaskbarItemInfo()
    do
        System.Windows.Application.Current.MainWindow.TaskbarItemInfo <- tbi
    let mutable ongoingSession : bool = false
    member x.DisplayProgressState state (goal : DailyGoalStatus option) =
            if Option.isSome goal then
                let g = Option.get goal
                match g.likelyhood with
                    | WillReachGoal.Likely -> tbi.ProgressState <- TaskbarItemProgressState.Normal
                    | WillReachGoal.UnLikely -> tbi.ProgressState <- TaskbarItemProgressState.Error
            else
                match state with
                    | NoOngoingSession -> tbi.ProgressState <- TaskbarItemProgressState.None
                    | RunningMoreOf -> tbi.ProgressState <- TaskbarItemProgressState.Normal
                    | RunningLessOf -> tbi.ProgressState <- TaskbarItemProgressState.Error
                    | RunningNeutral -> tbi.ProgressState <- TaskbarItemProgressState.Paused
                    | Stalled -> tbi.ProgressState <- TaskbarItemProgressState.Paused
    member x.ProgressPercentage 
        with set value = tbi.ProgressValue <- value
                
   