#nowarn "52"

namespace BucklingSprings.Aware.Common.Focus

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Utils

[<NoComparison()>]
type FocusSession =
    {
        idle : bool
        sessionClassId : ClassIdentifier
        startTime : System.DateTimeOffset
        endTime : System.DateTimeOffset
        words : int
    }
    interface NoiseReduction.ReducableActivity<FocusSession> with
        override x.StartDateTime = x.startTime
        override x.EndDateTime = x.endTime
        override x.IsInSameClass y = x.sessionClassId = y.sessionClassId
        override x.IsIdle = x.idle
        override x.Combine y = 
            let earlier, later = if x.startTime < y.startTime then x,y else y,x
            {
                idle = x.idle
                sessionClassId = x.sessionClassId
                startTime = earlier.startTime
                endTime = later.endTime
                words = if x.sessionClassId = y.sessionClassId then x.words + y.words else x.words
            }

[<NoComparison()>]
type OnGoingFocusSession =
    {
        current : FocusSession
        targetLengthInMinutes : int
        stalled: bool
    }
    

module FocusSessionCalculator =

    let toSession (idleMap : ClassIdentifier -> bool)  (sample :  WithDate<MeasureForClass<ActivitySummary>>) : FocusSession =
        let forClass' = Dated.unWrap sample
        let classId, summ = forClass'
        let sTime = Dated.dt sample
        {
            idle = idleMap classId
            sessionClassId = classId
            startTime = sTime
            endTime = sTime.AddMinutes(float summ.minuteCount)
            words = summ.wordCount
        }



    let targetForSession session stalled = 
        let targetIncrements = if stalled then 5 else 15
        let sessionLengthRoundedUp = (session.endTime - session.startTime).TotalMinutes |> ceil |> int
        ((sessionLengthRoundedUp / targetIncrements) + 1) * targetIncrements

    let toSessions (idleMap : ClassIdentifier -> bool) (samples : (WithDate<MeasureForClass<ActivitySummary>>) List) =
        samples
            |> List.map (toSession idleMap)
            |> NoiseReduction.combine
        

    let combineAndFlush (idleMap : ClassIdentifier -> bool) (forToday  : bool) (samples : (WithDate<MeasureForClass<ActivitySummary>>) List)  : FocusSession list * OnGoingFocusSession Option =
        let sessions = samples |> List.map (toSession idleMap) |> List.sortBy (fun s -> s.startTime)
        let combined = NoiseReduction.combine sessions
        let sameAsLastCombinedEntry x =
            match (List.rev combined) with
            | [] -> false
            | y :: _ -> y.sessionClassId = x.sessionClassId
        if forToday then
            let ongoing = NoiseReduction.ongoing sessions
            match ongoing with
            | (None, _) -> combined, None
            | (Some current, stalled) -> 
                if sameAsLastCombinedEntry current then
                    let last = List.head (List.rev combined)
                    combined, Some ({current = last; targetLengthInMinutes = targetForSession last false; stalled = false})
                else
                    combined, Some ({current = current; targetLengthInMinutes = targetForSession current stalled; stalled = stalled})
        else
            combined, None












