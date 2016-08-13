namespace BucklingSprings.Aware

open System.IO

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Goals
module Broadcast =

    type Progress = {
        taskName : string
        percentageDone : int
        minutes : int
        wordCount : int
        moreOrLess : MoreOf
    }
    
    type BroadcastState =
        | Idle
        | Stalled of string
        | Working of Progress

    let quote s = sprintf "\"%s\"" s

    let goalStatelaunchArguments (goalStatus : DailyGoalStatus option) =
        if Option.isSome goalStatus then
            let g = Option.get goalStatus
            let gl = sprintf "--goal-likely %s" (match g.likelyhood with
                                                            | WillReachGoal.Likely -> "Likely"
                                                            | WillReachGoal.UnLikely -> "Unlikely")
            let gw = sprintf "--goal-words %d" (g.wordGoal)
            let gwd = sprintf "--goal-words-done %d" (g.wordsToday)
            let glp = sprintf "--goal-likely-percent %d" (g.canReach)
            sprintf "%s %s %s %s" gl gw gwd glp
        else
            ""

    let broadcastStatelaunchArguments  =
        function
            | BroadcastState.Idle -> ""
            | BroadcastState.Stalled s -> sprintf "--task-name %s" (quote (sprintf "%s - Stalled" s))
            | BroadcastState.Working progress ->
                let t = sprintf "--task-name %s" (quote progress.taskName)
                let p = sprintf "--percentage-done %d" (progress.percentageDone)
                let m = sprintf "--minutes %d" progress.minutes
                let w = sprintf "--word-count %d" progress.wordCount
                let ml = match progress.moreOrLess with
                            | MoreOf.MoreOf -> "--more-or-less more"
                            | MoreOf.LessOf -> "--more-or-less less"
                            | MoreOf.Neutral -> "--more-or-less neutral"
                    
                sprintf "%s %s %s %s %s" t p m w ml

    let broadcast (s : BroadcastState) (goalStatus : DailyGoalStatus option) =
        let args = sprintf "%s %s" (broadcastStatelaunchArguments s) (goalStatelaunchArguments goalStatus)
        let executablePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        let fileName = "mbroadcast.exe"


        let exe = if Environment.currentEnvironment = Environment.Development then
                    System.IO.Path.Combine(executablePath, "..\\..\\..\\mbroadcast\\bin\\Debug\\", fileName)
                    else
                    System.IO.Path.Combine(executablePath, fileName)

        if File.Exists exe then
            use prc = new System.Diagnostics.Process()
            prc.StartInfo <- System.Diagnostics.ProcessStartInfo(exe)
            prc.StartInfo.Arguments <- args
            prc.Start() |> ignore
            ()

