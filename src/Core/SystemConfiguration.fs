namespace BucklingSprings.Aware.Core

open BucklingSprings.Aware.Core.Environment

module SystemConfiguration =
    
    let sampleTimeInMilliseconds = 60000

    let reclassifyTimeInMilliseconds = 
        match currentEnvironment with
            | Development -> 1000
            | Production -> 1000

    let catchUpSleepTimeNothingFoundLastTime = 
        match currentEnvironment with
            | Development -> 5000
            | Production -> 60000

    let catchUpQueuePollingInterval = 
        match currentEnvironment with
            | Development -> 1000
            | Production -> 1000


