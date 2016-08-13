namespace BucklingSprings.Aware.Collector

open System
open System.Windows
open System.Windows.Forms
open System.Diagnostics

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Store

open Nessos

module Program =

    type CollectorArguments =
        | Skip_Hooks
        | No_Collection_Delay
    with 
        interface UnionArgParser.IArgParserTemplate with
            member x.Usage =
                match x with
                | CollectorArguments.Skip_Hooks -> "Do not install mouse and keyboard hooks. (Only used for debugging)."
                | CollectorArguments.No_Collection_Delay -> "Skip initial delay before first sample"

    let startup skipHooks noCollectionDelay errorDisplayFn =
        if skipHooks then
            Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Hooks skipped."
        else
            Hooks.setInputHooks()
            Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Hooks setup."

        Activities.collect noCollectionDelay errorDisplayFn
            |> List.iter Async.Start

    let logConfig noCollectionDelay skipHooks =
        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "No Collection Delay = %A" noCollectionDelay
        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Skip Hooks %A" skipHooks

    [<EntryPoint()>]
    let Main args =

        Diagnostics.initialize EventLogSource.Collector
        Diagnostics.writeToLog EventLogger.Collector EntryLevel.Information "Collector starting up at %s" (System.DateTimeOffset.Now.ToString())

        let parser = UnionArgParser.UnionArgParser.Create<CollectorArguments>()

        try
            let splash = SplashScreen("CollectorSplash.jpg")
            splash.Show(false)
            
            let results = parser.ParseCommandLine(args)
            let skipHooks = results.Contains(<@ Skip_Hooks @>)
            let noCollectionDelay = results.Contains(<@ No_Collection_Delay @>)
            
            logConfig noCollectionDelay skipHooks
            
            let hiddenWindow, showErrr = SplashForm.instance

            Store.initialize (false)
            use tmr = new System.Windows.Forms.Timer()
            tmr.Interval <- 2000
            let startupAndDisableTimer _ =
                tmr.Enabled <- false
                splash.Close(TimeSpan.FromSeconds(1.0))
                startup skipHooks noCollectionDelay showErrr

            tmr.Tick.Add(startupAndDisableTimer)
            tmr.Enabled <- true
            System.Windows.Forms.Application.Run(hiddenWindow)
            0
        with
            | ex -> Diagnostics.writeToLog EventLogger.Collector EntryLevel.Error "Exception: %s" (ex.ToString()) ; -1
