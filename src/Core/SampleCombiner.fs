#nowarn "52"

namespace BucklingSprings.Aware.Core

open BucklingSprings.Aware.Core.Models

module SampleCombiner =

    type SampleNonCombinationReason =
        | DifferentIdleness
        | DifferentHour
        | DifferentWindow
        | DifferentProcess
        | NotCloseEnough
        | NoOldSample

    type SampleCombinationResult =
        | CombinedSample of UnsavedSample
        | NewSample of SampleNonCombinationReason

    let canCombineSamples (x : UnsavedSample) (y : UnsavedSample) : SampleNonCombinationReason option =
        let sameIdleNess = x.inputActivity.IsNotIdle = y.inputActivity.IsNotIdle
        let sameHour = (x.timeAndDate.Year = y.timeAndDate.Year) 
                            && x.timeAndDate.Month = y.timeAndDate.Month
                            && x.timeAndDate.Day = y.timeAndDate.Day
                            && x.timeAndDate.Hour = y.timeAndDate.Hour
        let sameWindow = x.windowTitle = y.windowTitle
        let sameProcess = x.processName = y.processName
        let closeEnough = abs (y.timeAndDate.Subtract(x.timeAndDate).TotalSeconds) < 70.0
       
        if not sameWindow then
            Some DifferentWindow
        elif not sameIdleNess then
            Some DifferentIdleness
        elif not sameHour then
            Some DifferentHour
        elif not closeEnough then
            Some NotCloseEnough
        elif not sameProcess then
            Some DifferentProcess
        else
            None

    let combineSamples (x : UnsavedSample) (y : UnsavedSample) =
        let combineActivity (a : InputActivity) (b:InputActivity) =
            {
                InputActivity.keyboardActivity = a.keyboardActivity + b.keyboardActivity
                InputActivity.mouseActivity = a.mouseActivity + b.mouseActivity
            }
        {y with inputActivity = (combineActivity x.inputActivity y.inputActivity)}

    let combine (lastSample : UnsavedSample) (sample : UnsavedSample) =
        let can =  canCombineSamples lastSample sample
        if Option.isNone can then
            SampleCombinationResult.CombinedSample (combineSamples lastSample sample)
        else
            SampleCombinationResult.NewSample (Option.get can)
        
            

    

