namespace BucklingSprings.Aware

open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Entitities

module Program = 

    type NameCountPair() =
        member val Name = System.String.Empty with get, set
        member val Count = 0 with get, set

    [<EntryPoint>]
    let main argv = 
        Store.initialize (false)
        use ctx = new AwareContext(Store.connectionString)
        let duplicateProgramNames = ctx.Database.SqlQuery<NameCountPair>("""select className as Name, count(*) as Count
                                                                            from ClassificationClasses
                                                                            where classifier_id = 1
                                                                            group by className
                                                                            having count(*) > 1""") |> Seq.toList
        if List.isEmpty duplicateProgramNames then
            printfn "All consistency checks passed."
        else
            printfn "Duplicate Program Names Found!"
            duplicateProgramNames
                |> List.iter (fun p -> printfn "\t %s - %d" p.Name p.Count)
        0 // return an integer exit code
