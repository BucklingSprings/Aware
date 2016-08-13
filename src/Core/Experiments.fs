namespace BucklingSprings.Aware.Core.Experiments

open System
open System.IO
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

type ExperimentKind =
    | OnOff
    | On
    | Off


type ExperimentDetails =
    {
        name : string
        startDate : DateTimeOffset
        kind: ExperimentKind
        durationInDays : int
    }


module Experiments =

    let storeRoot = Environment.defaultExperimentStoreRoot

    let parseDt (s : string) =
        let dt = ref (DateTimeOffset())
        if DateTimeOffset.TryParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, dt) then
            Some (!dt)
        else
            None

    let parseKind (s : string) =
        match s.ToUpperInvariant() with
            | "ONOFF" -> Some(ExperimentKind.OnOff)
            | "ON" -> Some(ExperimentKind.On)
            | "OFF" -> Some(ExperimentKind.Off)
            | _ -> None

    let parseDuration (s : string) =
        let dur = ref 0
        if System.Int32.TryParse(s, dur) then
            Some (!dur)
        else
            None



    let parseDetailsFromFileName fileName =
        let fi = FileInfo(fileName)
        let name = fi.Name
        let parts = name.Split([| '_' |])
        if parts.Length = 5 then
            let name = parts.[0]
            let startDateOpt = parseDt parts.[1]
            let kindOpt = parseKind parts.[2]
            let durationOpt = parseDuration parts.[3]
            if Option.isSome startDateOpt && Option.isSome kindOpt && Option.isSome durationOpt then
                Some {
                        name = name
                        startDate = Option.get startDateOpt
                        kind = Option.get kindOpt
                        durationInDays = Option.get durationOpt
                     }
            else
                None
        else
            None

    let toDescription k =
        match k with
            | ExperimentKind.OnOff -> "Randomize Aware startup"
            | ExperimentKind.Off -> "Never start Aware"
            | ExperimentKind.On -> "Always start Aware"

    let toShortDescription k =
        match k with
            | ExperimentKind.OnOff -> "OnOff"
            | ExperimentKind.Off -> "Off"
            | ExperimentKind.On -> "On"

    let toFileName (expDetails : ExperimentDetails) =
        let k = toShortDescription expDetails.kind

        let startOfDay = expDetails.startDate.StartOfDay
        sprintf "%s_%s_%s_%d_exp.csv" expDetails.name (startOfDay.ToString("yyyy-MM-dd")) k expDetails.durationInDays

    let fullPath (expDetails : ExperimentDetails) =
        Path.Combine(storeRoot, toFileName expDetails)
        

    let listAllExperiments () =
        if Directory.Exists storeRoot then
            Directory.GetFiles(storeRoot, "*exp.csv")
                |> Array.choose parseDetailsFromFileName
                |> Array.toList
        else
            List.empty

    let startNewExperiment (expDetails : ExperimentDetails) =
        if not (Directory.Exists storeRoot) then
            Directory.CreateDirectory storeRoot |> ignore
        let f = fullPath expDetails
        if File.Exists f then
            false
        else
            let rand = Random(let now = DateTimeOffset.Now in now.Millisecond)
            use writer = new StreamWriter(f, false, new Text.UTF8Encoding(), 1024)
            writer.WriteLine("Date,Outcome")
            for i in 1 .. expDetails.durationInDays do
                let d = let dt = expDetails.startDate.AddDays(float i) in dt.ToString("yyyy-MM-dd")
                let outcome = if rand.Next(0,2) = 0 then "Head" else "Tails"
                let row = sprintf "%s,%s" d outcome
                writer.WriteLine row
            true

    let experimentInProgressOn (dt : DateTimeOffset) =
        listAllExperiments ()
            |> List.tryPick (fun e ->
                                let startDt = e.startDate
                                let endDt = startDt.AddDays(float e.durationInDays)
                                if dt >= startDt && dt <= endDt then
                                    Some e
                                else
                                    None)
    let abandonRunningOnDate (dt : DateTimeOffset) =
        let e = experimentInProgressOn dt
        if Option.isSome e then
            let file = fullPath (Option.get e)
            if File.Exists file then
                File.Delete file

    let outcomeForDay (dt : DateTimeOffset) (e : ExperimentDetails) = 
        let parse (s : string) = 
            let parts = s.Split([|','|])
            if parts.Length = 2 then
                let dt = DateTimeOffset.ParseExact(parts.[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                dt, (parts.[1].Equals("Head", StringComparison.InvariantCultureIgnoreCase))
            else
                failwith "Malformed experiment file"
            
        use reader = new StreamReader(fullPath e)
        reader.ReadLine() |> ignore // skip header

        let outcomes = seq {
                            while reader.EndOfStream = false do
                                let line = reader.ReadLine()
                                yield (parse line)}
        let outcome = outcomes |> Seq.tryFind (fun (d,o) -> d.IsSameDay(dt))
        if Option.isSome outcome then
            snd (Option.get outcome)
        else
            failwith "Wrong query for outcome"

    let runningOnDate (dt : DateTimeOffset) (e : ExperimentDetails) =
        let s = let start = e.startDate in start.AddDays(1.0)
        let e = s.AddDays(float e.durationInDays)
        dt >= s && dt <= e
         
    let canStartAware (dt : DateTimeOffset) =
        let exp = experimentInProgressOn dt
        if Option.isSome exp then
            let e = Option.get exp
            if e.startDate.IsSameDay(dt) then
                    true
                else
                    match e.kind with
                        | ExperimentKind.Off -> false
                        | ExperimentKind.On -> true
                        | ExperimentKind.OnOff -> outcomeForDay dt e
        else
            true
