namespace BucklingSprings.Aware.Core

open System.IO

module HouseKeeping =

    let startUpAsync () =
        let tryDel (path : string) =
            try
                Directory.Delete(path, true) 
            with
                | _ -> ()
        async {
            do! Async.Sleep(2000)
            Directory.GetDirectories(Path.GetTempPath(), "AwareReplay*")
                |> Seq.iter tryDel
            
        }

