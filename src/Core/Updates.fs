namespace BucklingSprings.Aware.Core

open System
open System.Net.Http

open Newtonsoft.Json

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics

module Updates =

    [<CLIMutable>]
    [<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
    type UpgradeInformation = {
        InformationUrl : string
        DownloadUrl : string
        Version : string
    }

    [<CLIMutable>]
    [<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
    type InstallInformation = {
        Product : string
        Version : string
        SerialNumber : string
    }

    let urlBases = [
            "https://bssws.apphb.com";
            "https://bssws.bucklingsprings.com"
        ]

    let checkUrls (installed : InstallInformation)  =
        urlBases
            |> List.map (fun s -> sprintf "%s/upgrade/%s/%s/%s" s installed.Product installed.Version installed.SerialNumber)

    let update (url : string) : UpgradeInformation option =
        try
            Diagnostics.writeToLog EventLogger.Updater EntryLevel.Information "Checking for updates %s" url
            use http = new HttpClient()
            let s = http.GetStringAsync(url).Result
            let updateInformation = JsonConvert.DeserializeObject<UpgradeInformation>(s)
            if String.IsNullOrWhiteSpace(updateInformation.DownloadUrl) || String.IsNullOrWhiteSpace(updateInformation.InformationUrl) then
                None
            else
                Some updateInformation
        with
            | ex ->
                Diagnostics.writeToLog EventLogger.Updater EntryLevel.Error "Error checking for updates %s - %s" url ex.Message
                None
        

    let pickUpdate (upgrade : UpgradeInformation option) : UpgradeInformation option =
        if Option.isNone upgrade then None else upgrade

    let updateAvailable (installed : InstallInformation) : UpgradeInformation option =
        match Environment.currentEnvironment with
        | Environment.Development -> None
        | Environment.Production ->
            let updates = checkUrls installed
                            |> List.map update
            updates |> List.tryPick pickUpdate

    let updateAvailableAsync (installed : InstallInformation) (onUpdate : UpgradeInformation -> Unit) =
        async {
                do! Async.Sleep(10000)
                let upgrade = updateAvailable installed
                if Option.isSome upgrade then
                    onUpdate (Option.get upgrade)
                return ()
        }
