namespace BucklingSprings.Aware.Core

open System.IO

module Environment =

    type Environment =
        | Development
        | Production

    let currentEnvironment =
        
        let executablePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        let env = if executablePath.ToUpperInvariant().Contains("""\SRC\""") then
                    Development
                  else
                    Production
        env

    let databaseLocation = 
        match currentEnvironment with
        | Development -> """c:\aware_dev"""
        | Production -> System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)

    let modelLocation = 
        match currentEnvironment with
        | Development -> """c:\aware_dev"""
        | Production -> System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)

    let databaseName = 
        match currentEnvironment with
        | Development -> "aware_dev"
        | Production -> "aware"


    let defaultDeepInspectionStoreRoot = 
        match currentEnvironment with
        | Development -> """c:\aware_dev\AwareBinaryStore\"""
        | Production -> Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "AwareReplay")

    let defaultExperimentStoreRoot = 
        match currentEnvironment with
        | Development -> Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "AwareDev", "AwareExperiments")
        | Production -> Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "AwareExperiments")

    let defaultAddOnStoreRoot = 
        let appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData, System.Environment.SpecialFolderOption.None)
        match currentEnvironment with
                    | Development -> Path.Combine(appData, "AwareDev", "AwareAddOns")
                    | Production -> Path.Combine(appData, "AwareAddOns")


    let connectionString = 
        sprintf "Data Source=(LocalDB)\\v11.0;AttachDbFilename=%s\\awareDb.mdf;Integrated Security=True;Database=%s" databaseLocation databaseName
