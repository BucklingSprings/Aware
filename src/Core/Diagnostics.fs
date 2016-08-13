namespace BucklingSprings.Aware.Core

open System
open System.IO
open System.Diagnostics

open log4net
open log4net.Core
open log4net.Layout;
open log4net.Appender
open log4net.Repository.Hierarchy


module Diagnostics =

    type EventLogSource =
        | Collector
        | Upgrade
        | Aware
        | MBroadcast
        | DeepSamples

    type EventLogger =
        | Collector
        | Upgrade
        | Aware
        | Updater
        | MBroadcast
        | DeepSamples

    type EntryLevel=
        | Error
        | Information
        | Debug

    let trace fmt  = Printf.ksprintf Trace.WriteLine fmt

    let verbose() =
        try
            let productName = match Environment.currentEnvironment with
                                | Environment.Development -> "AwareDev"
                                | _ -> "Aware"

            let key = sprintf "HKEY_CURRENT_USER\\Software\\BucklingSprings\\%s" productName
            if Microsoft.Win32.Registry.GetValue(key, "verbose", null) = null then
                false
            else
                true
        with
        | ex -> true

    let initialize src =
        
        let prefix = match BucklingSprings.Aware.Core.Environment.currentEnvironment with
                        | Environment.Development -> "Dev_"
                        | _ -> String.Empty
        
        let suffix = 
            match src with
                | EventLogSource.Collector -> "Aware.Collector.log"
                | EventLogSource.Upgrade -> "Aware.Upgrade.log"
                | EventLogSource.Aware -> "Aware.Ui.log"
                | EventLogSource.MBroadcast -> "Aware.MBroadcast.log"
                | EventLogSource.DeepSamples -> "Aware.Deep.log"

        let logFileName = sprintf "%s%s" prefix suffix
        

        let hierarchy : Hierarchy  = log4net.LogManager.GetRepository() :?> Hierarchy
        let patternLayout = PatternLayout()
        patternLayout.ConversionPattern <- "%date [%thread] %-5level %logger - %message%newline"
        patternLayout.ActivateOptions()

        let roller = new RollingFileAppender()
        roller.AppendToFile <- false
        roller.File <- Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), logFileName))
        roller.Layout <- patternLayout
        roller.MaxSizeRollBackups <- 5
        roller.MaximumFileSize <- "1GB";
        roller.RollingStyle <- RollingFileAppender.RollingMode.Size
        roller.AppendToFile <- true
        roller.StaticLogFileName <- true;           
        roller.ActivateOptions()
        hierarchy.Root.AddAppender(roller);

        let memory = new MemoryAppender();
        memory.ActivateOptions();
        hierarchy.Root.AddAppender(memory);

        if verbose() then
            hierarchy.Root.Level <- Level.All;
        else
            hierarchy.Root.Level <- Level.Info

        hierarchy.Configured <- true;
        ()

    let writeToLog logger level fmt = 
        let write s =
            use el = new System.Diagnostics.EventLog("Aware")
            let lg = match logger with
                        | EventLogger.Collector -> LogManager.GetLogger("Aware.Collector")
                        | EventLogger.Upgrade -> LogManager.GetLogger("Aware.Upgrade")
                        | EventLogger.Aware -> LogManager.GetLogger("Aware.Ui")
                        | EventLogger.Updater -> LogManager.GetLogger("Aware.Updater")
                        | EventLogger.MBroadcast -> LogManager.GetLogger("Aware.MBroadcast")
                        | EventLogger.DeepSamples -> LogManager.GetLogger("Aware.DeepSamples")

            
            match level with
                | EntryLevel.Error -> lg.Error s
                | EntryLevel.Information -> lg.Info s
                | EntryLevel.Debug -> lg.Debug s

        Printf.ksprintf write fmt