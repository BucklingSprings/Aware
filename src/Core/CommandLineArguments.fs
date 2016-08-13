namespace BucklingSprings.Aware.Core

open System


module CommandLineArguments =

    let argumentMap =
        let pairs = Seq.map (fun s -> (s, s)) (Environment.GetCommandLineArgs())
        Map(pairs)

    let containsFlag f = argumentMap.ContainsKey f


