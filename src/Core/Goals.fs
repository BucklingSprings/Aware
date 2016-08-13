namespace BucklingSprings.Aware.Core.Goals

open System

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models

type DailyGoalConfig =
    {
        enabled : bool
        wordGoal : int
        automaticGoalSetting : bool
        wordsToday : int
    }

type WillReachGoal =
    | Likely
    | UnLikely

type DailyGoalStatus =
    {
        wordGoal : int
        wordsToday : int
        minutesToday : int
        canReach : int // in percent from 0 to 100
        likelyhood : WillReachGoal
    }




module GoalConfigStore =

    let productName = match Environment.currentEnvironment with
                                | Environment.Development -> "AwareDev"
                                | _ -> "Aware"

    let key = sprintf "HKEY_CURRENT_USER\\Software\\BucklingSprings\\%s\\Goals" productName

    let intValue (valueName : string) (defaultValue : int) : int =
        let value = Microsoft.Win32.Registry.GetValue(key, valueName, null)
        if value <> null then
            downcast value
        else
            defaultValue

    let datedIntValue (dt : DateTimeOffset) (valueName : string) (defaultValue : int) : int =
        let datedKey = sprintf "%s\\Date%s" key (dt.ToString("MMddyyyy"))
        let value = Microsoft.Win32.Registry.GetValue(datedKey, valueName, null)
        if value <> null then
            downcast value
        else
            defaultValue

    let strValue (valueName : string) (defaultValue : string) : string =
        let value = Microsoft.Win32.Registry.GetValue(key, valueName, null)
        if value <> null then
            downcast value
        else
            defaultValue

    let setIntValue (valueName : string) (value : int)  =
        Microsoft.Win32.Registry.SetValue(key, valueName, value)

    let deleteAllDated (dt : DateTimeOffset) (valueName : string) =
        let sw = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software")
        if sw <> null then
            let bs = sw.OpenSubKey("BucklingSprings")
            if bs <> null then
                let product = bs.OpenSubKey(productName)
                if product <> null then
                    let goals = product.OpenSubKey("Goals", true)
                    if goals <> null then
                        goals.GetSubKeyNames()
                            |> Seq.iter (fun v -> if v.StartsWith("Date") then goals.DeleteSubKey(v))

        ()

    let setDatedIntValue (dt : DateTimeOffset) (valueName : string) (value : int)  =
        deleteAllDated dt valueName
        let datedKey = sprintf "%s\\Date%s" key (dt.ToString("MMddyyyy"))
        Microsoft.Win32.Registry.SetValue(datedKey, valueName, value)

    let setStrValue (valueName : string) (value : string)  =
        Microsoft.Win32.Registry.SetValue(key, valueName, value)


    let goalConfig (dt : DateTimeOffset) =
        {
            enabled = (intValue "Enabled" 1) = 1
            wordGoal = intValue "WordGoal" 2000
            automaticGoalSetting = (intValue "Automatic" 1) = 1
            wordsToday = datedIntValue dt "WordsToday" 0
        }

    let storeGoalConfig (dt : DateTimeOffset) (cfg :DailyGoalConfig) =
        setIntValue "Enabled" (if cfg.enabled then 1 else 0)
        if cfg.automaticGoalSetting then
            setIntValue "Automatic" 1
            setIntValue "WordGoal" 0
        else
            setIntValue "Automatic" 0
            setIntValue "WordGoal" cfg.wordGoal
        setDatedIntValue dt "WordsToday" cfg.wordsToday

module Goals =

    let goalForDay (dt : DateTimeOffset) (automaticGoal : int)  : int option =
        let cfg = GoalConfigStore.goalConfig dt
        if cfg.enabled then
            if cfg.wordsToday > 0 then
                Some (cfg.wordsToday)
            elif cfg.automaticGoalSetting = false && cfg.wordGoal > 0 then
                Some (cfg.wordGoal)
            elif automaticGoal > 0 then
                Some (automaticGoal)
            else
                None
        else
            None
        
    
module GoalCalculator =

    let automaticGoal (historicalPeformance : DailyPerformance) = int ((float historicalPeformance.words) * 1.10)

    let canReach (goal : int) (todaysPerformance : DailyPerformance) (historicalPeformance : DailyPerformance) =
        if historicalPeformance.minutes < 1 || historicalPeformance.words < 1  then
            // if there is no historical performance - chances same as flipping a coin
            50
        else    
            let timeLeft' = historicalPeformance.minutes - todaysPerformance.minutes
            let timeLeft = if timeLeft' < 0 then 10 else timeLeft' // assume the user will work atleast 5 more minutes
            let hWpm = (float historicalPeformance.words) / (float historicalPeformance.minutes)
            let todaysWpm = if todaysPerformance.minutes > 0 then ((float todaysPerformance.words) / (float todaysPerformance.minutes)) else hWpm
            let wpm = max hWpm todaysWpm
            let probablMoreWords = int ( (float timeLeft) * wpm)
            let estimatedTotal = todaysPerformance.words + probablMoreWords
            if estimatedTotal >= goal then
                100
            else
                let shortFall = goal - estimatedTotal
                let neededWpm = ((float shortFall) / (float timeLeft))
                if neededWpm < wpm then
                    100
                else
                    let wpmShortFall = neededWpm - wpm
                    let pctWpmShortFall = int (100.0 * wpmShortFall / wpm)
                    let p = 100 - pctWpmShortFall
                    if p < 0 then
                        0
                    elif p > 100 then
                        100
                    else
                        p
            
        

    let goalStatus (todaysPerformance : DailyPerformance) (historicalPeformance : DailyPerformance)  (dt : DateTimeOffset) =
        let cfg = GoalConfigStore.goalConfig dt
        if cfg.enabled then
            let goal =
                if cfg.wordsToday > 0 then
                    cfg.wordsToday
                elif cfg.automaticGoalSetting then
                    automaticGoal historicalPeformance
                else
                    cfg.wordGoal
            if goal > 0 then
                let reachedAlready = todaysPerformance.words >= goal
                let reachPct = if reachedAlready then 100 else canReach goal todaysPerformance historicalPeformance 
                Some {wordGoal = goal
                      wordsToday = todaysPerformance.words
                      canReach = reachPct
                      minutesToday = todaysPerformance.minutes
                      likelyhood = if reachPct >= 75 then WillReachGoal.Likely else WillReachGoal.UnLikely}
            else
                None
        else
            None
        
        
