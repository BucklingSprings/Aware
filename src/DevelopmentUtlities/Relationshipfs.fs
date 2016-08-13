namespace BucklingSprings.Aware.Store.Development.Utlities

module Relationships =

    type LevelOfDetail =
        | Daily
        | Hourly

    type DateRandomVariableKind =
        | DayOfWeek
        | HourOfDay
        | HourOfWork

    type NumericRandomVariablesKind =
        | Productivity
        | Sleep
        | Exercise
        | StartTime
        | EndTime
        | IdleTime
        | NumberOfBreaks


    type RandomVariable = 
        | NumericRandomVariable of NumericRandomVariablesKind * LevelOfDetail
        | DateRandomVariable of DateRandomVariableKind

    let humanize = 
        function
            | NumericRandomVariable(k, _) -> sprintf "%A" k
            | DateRandomVariable(k) -> sprintf "%A" k

    let humanizeR (rv1,rv2) = 
        let sorted = Seq.sort [humanize rv1; humanize rv2] |> Seq.toList |> List.rev
        let x = Seq.head sorted
        let y = Seq.nth 1 sorted
        sprintf "%s %s" x y
        

    let canMix levelOfDetail dateKind =
        match (levelOfDetail, dateKind) with
        | (LevelOfDetail.Daily, DateRandomVariableKind.DayOfWeek) -> true
        | (LevelOfDetail.Hourly, DateRandomVariableKind.HourOfDay) -> true
        | (LevelOfDetail.Hourly, DateRandomVariableKind.HourOfWork) -> true
        | _ -> false


    let canCoRelate x y =
        if x = y then
            false
        else match (x, y) with
                | (RandomVariable.NumericRandomVariable(nk, ld), RandomVariable.DateRandomVariable(dk)) -> canMix ld dk
                | (RandomVariable.DateRandomVariable(_), RandomVariable.DateRandomVariable(_)) -> false
                | (RandomVariable.NumericRandomVariable(nk1, ld1), RandomVariable.NumericRandomVariable(nk2, ld2)) -> (nk1 <> nk2) && (ld1 = ld2)
                | _ -> false
            
    let numericRvs = 
        let rv k ld = NumericRandomVariable(k, ld)
        [
            rv Productivity Hourly;
            rv Productivity Daily;
            rv Sleep Daily;
            rv Exercise Daily;
            rv StartTime Daily;
            rv EndTime Daily;
            rv IdleTime Daily;
            rv NumberOfBreaks Daily;
        ]

    let dateRvs =
        [
            DateRandomVariable DayOfWeek;
            DateRandomVariable HourOfDay;
            DateRandomVariable HourOfWork
        ]

    let allRvs = List.append numericRvs dateRvs

    let possibleRelationships =
        [
            for x in allRvs do
                for y in allRvs do
                    if canCoRelate x y then
                        yield (x, y)
        ]
        



    


