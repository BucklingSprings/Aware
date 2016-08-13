namespace BucklingSprings.Aware.Exporter

open System
open System.IO

open BucklingSprings.Aware.Core.Experiments

module ExperimentListExporter =

    let exportToFile (startDate : DateTimeOffset) (endDate : DateTimeOffset) fileName =
        use writer = new StreamWriter(fileName, false, new Text.UTF8Encoding(), 1024)
        writer.WriteLine("Name,StartDate,DurationInDays,DataFile,Kind")
        let exps = Experiments.listAllExperiments ()
        exps
            |> List.iter (fun e ->
                                    let n = e.name
                                    let d = Export.formatDateOnly e.startDate
                                    let dur = string e.durationInDays
                                    let f = (Experiments.fullPath e)
                                    let k = match e.kind with
                                                | ExperimentKind.OnOff -> "Randomize Aware"
                                                | ExperimentKind.On -> "Always On"
                                                | ExperimentKind.Off -> "Always Off"
                                    let row = sprintf "%s,%s,%s,%s,%s" n d dur f  k
                                    writer.WriteLine row)
        List.length exps
        

