#nowarn "52"

namespace BucklingSprings.Aware.Store.Tests

open BucklingSprings.Classification

module Program =

    let dumpClassPriors m =
        printfn "Class Priors"
        Classifier.classPriors m
            |> Seq.iter (fun (c,p) -> printfn "%A %f" c p)

    let dumpPredictions (fs : list<string>) m =
        let s = System.String.Join(",",fs)
        printfn "  Prediction for %s" s
        Classifier.predict (Set.ofList fs) m
            |> Seq.sortBy snd
            |> Seq.iter (fun (c,p) -> printfn "          %s %f" c p)
        

    [<EntryPoint>]
    let main argv = 
        let cls = [
                        "Learning"
                        "Other"
                        "Personal"
                        "Slacking"
                        "Work - ATG"
                        "Work - BSS" ]
        let m = Classifier.create (Set.ofList cls)
        let e1 = [
                    ""
                    "2012"
                    "aware"
                    "microsoft"
                    "studio"
                    "visual"
                    ]
        let e2 = [
                    ""
                    "2012"
                    "app"
                    "microsoft"
                    "studio"
                    "truvisit"
                    "visual"
                    ]
        dumpClassPriors m
        dumpPredictions e1 m
        let m = Classifier.trainWeighted (Set.ofList e1) 3263 "Work - BSS" m
        printfn "\n\nAfter training e1\n\n**********"
        dumpClassPriors m
        dumpPredictions e2 m
        let m = Classifier.trainWeighted (Set.ofList e2) 2308 "Work - BSS" m
        printfn "\n\nAfter training e2\n\n**********"
        dumpClassPriors m
        let m = Classifier.undoWeighted (Set.ofList e2) 2308 "Work - BSS" m
        assert (Option.isSome m)
        let m = Option.get m
        printfn "\n\nAfter undo e2\n\n**********"
        dumpClassPriors m
        dumpPredictions e2 m
        0