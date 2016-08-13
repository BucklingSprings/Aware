#if COMPILED
namespace BucklingSprings.Aware.Core
#endif

module NoiseReduction =

    type ReducableActivity<'a> =
        abstract StartDateTime : System.DateTimeOffset with get
        abstract EndDateTime : System.DateTimeOffset with get
        abstract IsInSameClass : 'a -> bool
        abstract Combine : 'a -> 'a
        abstract IsIdle : bool
    
    let size (x : #ReducableActivity<_>) = 
        let ts = x.EndDateTime - x.StartDateTime
        ts.TotalMinutes |> ceil |> int

    let distance (x : #ReducableActivity<_>) (y : #ReducableActivity<_>) =
        let earlier, later = if x.StartDateTime < y.StartDateTime then x,y else y,x
        let ts = later.StartDateTime - earlier.EndDateTime
        ts.TotalMinutes |> ceil |> int


    let canXTakeOverY strict x y = 
        let distractionPeriodAllowed = 14
        let sizeX = size x
        let sizeY = size y
        let distance = distance x y
        let areSame = x.IsInSameClass y
        if areSame then
            (distance < distractionPeriodAllowed) && (distance < sizeX + sizeY)
        else
            if strict then
                false
            else
                (sizeX > sizeY) && (distance < sizeX) && (distance < distractionPeriodAllowed) && (sizeY < distractionPeriodAllowed)
        

    let rec canCombineWithHead strict ongoing rest =
        match rest with
        | [] -> false
        | head :: rest ->  if canXTakeOverY strict ongoing head then true else canCombineWithHead strict ongoing rest

    let rec combineWithHead strict (ongoing : #ReducableActivity<_>) rest =
        match rest with
        | [] -> ongoing, rest
        | head :: rest ->  (ongoing.Combine head, rest)

    let rec reduce' strict combinedSoFar ongoing activities =
        if canCombineWithHead strict ongoing activities then
            let combined, rest = combineWithHead strict ongoing activities
            reduce' strict combinedSoFar combined rest
        else
            List.concat [combinedSoFar; [ongoing]; (reduce strict activities)]
    and reduce strict (samples : #ReducableActivity<_> list)  =
        match samples with
        | [] -> []
        | h :: rest -> reduce' strict [] h rest

    let combine activities =
        // 4 Pass reduction: strict - strict reverse - unstrict - unstrict reversed
        activities |> reduce true |> List.rev |> reduce true |> reduce false |> List.rev |> reduce false

    let ongoing activities =
        let last2 = activities |> List.rev |> Seq.truncate 2  |> List.ofSeq
        let allowedStallLength = 2
        match last2 with
        | [] -> (None, false)
        | [x] -> (Some x, false)
        | [x ; y] -> if size x > allowedStallLength || y.IsIdle then
                        (Some x, false)
                      elif size x > size y then
                        (Some x, false)
                      else (Some y, true)
        | _ -> failwith "impossible"
        
        
#if INTERACTIVE          

    type TestActivity(s, e, c : string) =
        member x.Class = c
        interface ReducableActivity<TestActivity> with
            override x.StartDateTime = s 
            override x.EndDateTime = e
            override x.IsInSameClass y = x.Class = y.Class
            override x.IsIdle = false
            override x.Combine y = 
                let xReducable = x :> ReducableActivity<_>
                let yReducable = y :> ReducableActivity<_>
                let earlier, later = if xReducable.StartDateTime < yReducable.StartDateTime then xReducable,yReducable else yReducable,xReducable
                TestActivity(earlier.StartDateTime, later.EndDateTime, x.Class)
  

    let testDebug () =
        let act s e c = 
            let dt x = System.DateTimeOffset.Parse(sprintf "2/1/2013 %s AM" x)
            TestActivity(dt s,dt e,c)
        
        let printA (a : TestActivity) = 
            let r = a :> ReducableActivity<_>
            let s = r.StartDateTime
            let e = r.EndDateTime
            let sz = size r
            printfn "%s - %s - %s (%d)" (s.ToString("hh:mm")) (e.ToString("hh:mm")) a.Class sz

        let run activities = 
            printfn ""
            printfn "Input"
            activities |> List.iter printA
            let reduced = combine activities
            printfn "Output"
            reduced |> List.iter printA
            printfn ""

        run [
                act "5:00" "5:02" "Red"
                act "5:02" "6:09" "Green"
                act "6:09" "6:10" "Blue"
                act "6:10" "6:53" "Blue"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:01" "6:10" "Green"
                act "6:10" "6:11" "Red"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:10" "6:10" "Green"
                act "6:12" "6:15" "Red"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:01" "5:02" "Green"
                act "5:02" "5:03" "Blue"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:01" "5:02" "Green"
                act "5:02" "5:03" "Red"
            ]
        run [
            ]
        run [
                act "5:00" "5:01" "Red"
                act "6:00" "6:01" "Red"
            ]

        run [
                act "5:00" "5:01" "Red"
                act "5:01" "6:01" "Red"
                act "6:01" "6:02" "Green"
                act "6:02" "6:06" "Green"
                act "6:06" "6:07" "Blue"
                act "6:07" "6:17" "Green"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:01" "6:01" "Red"
                act "6:01" "6:02" "Green"
                act "6:06" "6:07" "Blue"
                act "6:07" "6:17" "Green"
            ]
        run [
                act "5:00" "5:01" "Red"
                act "5:06" "5:08" "Red"
            ]
        run [
                act "4:00" "5:01" "Red"
                act "5:06" "5:08" "Red"
            ]
        run [
                act "4:00" "5:00" "Red"
                act "6:00" "7:00" "Red"
            ]
        // This example is probably the best example of things wrong with this combinaiton technique
        // Currently does
        //        Input
        //        04:00 - 05:00 - Red (60)
        //        05:00 - 05:05 - Green (5)
        //        05:05 - 05:06 - Red (1)
        //        05:06 - 06:06 - Green (60)
        //        Output
        //        04:00 - 05:06 - Red (66)
        //        05:06 - 06:06 - Green (60)
        // Would Be Nicer to output
        //        04:00 - 05:00 - Red (60)
        //        05:00 - 06:06 - Green (66)
        run [
                act "4:00" "5:00" "Red"
                act "5:00" "5:05" "Green"
                act "5:05" "5:06" "Red"
                act "5:06" "6:06" "Green"
            ]
        run [
                act "4:00" "5:00" "Red"
                act "5:00" "5:05" "Green"
                act "5:05" "5:06" "Red"
                act "5:06" "6:06" "Green"
                act "6:06" "6:07" "Red"
            ]
        ()



#endif