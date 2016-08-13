namespace BucklingSprings.Aware.Collector

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Classifiers
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Common.Summarizers
open BucklingSprings.Aware.Common.CatchUp

module Activities =

    

    let collect noCollectionDelay errorDisplay = 

        let wrap a (s : string) erroVal =
            async {
                let! r = Async.Catch(a)
                let v = match r with
                            | Choice1Of2 n -> n
                            | Choice2Of2 ex ->
                                                errorDisplay s
                                                Diagnostics.writeToLog EventLogger.Collector EntryLevel.Error "%s" s
                                                Diagnostics.writeToLog EventLogger.Collector EntryLevel.Error "%s" (ex.ToString())
                                                erroVal
                return v
            }

        let catchUpMailBox = new MailboxProcessor<CatchUpMessage>(fun inbox ->
            let classifiers = ClassifierStore.loadAllClassifiers()
            let classify (classifiersToUse : IClassifier list, sample : ActivitySample) =
                let classified = classifiersToUse |> List.map (fun c -> c.Classify sample) 
                ClassifierStore.saveClassification sample classified
            let summarizeDay d  = 
                let samples = ActivitySamplesStore.samplesForDay d
                let allClasses = ClassifierStore.allClasses()
                let allClassifiers = ClassifierStore.allClassifiers()
                let summaries = DailySummarizer.summarize d samples allClassifiers allClasses
                SummaryStore.storeDaySummaries d summaries

            let rec loop count =
                let a = async { 
                        let! msg = inbox.Receive()
                        match msg with
                            | InitialClassification w -> classify(classifiers |> List.map snd, w)
                            | ReClassify (classifierToUse, w) -> classify(classifiers |> List.filter (fun (cId, _) -> cId = classifierToUse.id) |> List.map snd, w)
                            | DailySummary d -> summarizeDay d
                        return! loop( count + 1) }
                wrap a "Collector Error: (catchup mailbox failure)" ()
            loop 0)

        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information  "Starting catch up mailbox"
        catchUpMailBox.Start()

        let sampleMailbox = new MailboxProcessor<UnsavedSample>(fun inbox ->
            let rec loop count lastMessage =
                let l = async { 
                        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Debug  "Message count = %d. Waiting for next message." count
                        let! msg = inbox.Receive()
                        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Debug  "Message received: %A" msg
                        let deepSample = async {
                            try
                                DeepInspection.sampleAll msg.timeAndDate
                            with
                                | ex -> Diagnostics.writeToLog EventLogger.Collector EntryLevel.Error  "Deep sampling error: %s" (ex.Message)
                        }
                        if msg.inputActivity.IsNotIdle then
                            // Capture deep samples only when the user is not idle
                            Async.Start deepSample
                        let (newSample, state) = SampleStore.save msg lastMessage
                        if newSample then
                            let actSample, _ = state
                            catchUpMailBox.Post (InitialClassification actSample)
                        return! loop( count + 1) (Some state) }
                wrap l "Collector Error: (sample mailbox failure)" ()
            loop 0 None)
        sampleMailbox.Start()

        let activityCollector =
            async {
                if not noCollectionDelay then
                    do! Async.Sleep(SystemConfiguration.sampleTimeInMilliseconds) // On normal start up start collecting to ease startup system burden
                while true do
                    let input = Hooks.getAndClearActivity ()
                    let wind = WindowInformation.getForeGroundWindowInformation ()
                    let now = System.DateTimeOffset.Now

                    Diagnostics.writeToLog EventLogger.Collector EntryLevel.Debug  "%A %A %A" now input wind

                    sampleMailbox.Post({timeAndDate = now; inputActivity = input; processName = (fst wind); windowTitle = (snd wind)})
                    do! Async.Sleep(SystemConfiguration.sampleTimeInMilliseconds)
                    ()
                return ()
            }

        let catchup =
            let processBatch msgs =
               let p = async {
                                while (catchUpMailBox.CurrentQueueLength > 0) do
                                    do! Async.Sleep(SystemConfiguration.catchUpQueuePollingInterval)
                                msgs |> List.iter (fun m -> catchUpMailBox.Post m)
                                while (catchUpMailBox.CurrentQueueLength > 0) do
                                    do! Async.Sleep(SystemConfiguration.catchUpQueuePollingInterval)
               }
               wrap p "Collector Error: (process batch failure)" ()
               
                
            async {
                try
                    try
                        while true do
                            do! Async.Sleep(SystemConfiguration.reclassifyTimeInMilliseconds)
                            let batch = ClassAssignmentCatchUp.batch()
                            match batch with
                                | CatchUpBatch.CaughtUp -> 
                                    Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Class Assignment is Caught Up"
                                    let batch = DailySummaryCatchup.summaryCatchUp (DailySummarizer.summarizerSetVersion (ClassifierStore.allClassifierVersions ()))
                                    match batch with
                                        | CatchUpBatch.CaughtUp -> do! Async.Sleep(SystemConfiguration.catchUpSleepTimeNothingFoundLastTime)
                                        | CatchUpBatch.Batch msgs -> do! processBatch msgs 
                                | CatchUpBatch.Batch msgs ->
                                    Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Class Assignment Batch Size: %d" (List.length msgs)
                                    do! processBatch msgs
                        return ()
                    with
                        | ex -> Diagnostics.writeToLog EventLogger.Collector EntryLevel.Error "Catchup Error: %s %s" ex.Message (ex.StackTrace.ToString())
                finally
                    trace "Exiting collector"
                    System.Windows.Forms.Application.Exit() 
            }
        [
            wrap activityCollector "Collector Error: (activity collection error)." ()
            wrap catchup "Collector Error: (catch error)." ()
        ]
