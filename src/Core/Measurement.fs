namespace BucklingSprings.Aware.Core.Measurement



open BucklingSprings.Aware.Core.Models

type ClassIdentifier = ClassIdentifier of int

type MeasureForClass<'a> = ClassIdentifier * 'a

[<RequireQualifiedAccess()>]
type MeasureByClass<'a> =
    | TotalAcrossClasses of 'a
    | ForClass of ClassIdentifier * 'a

module ByClass =

    let chooseTotal bc =
        match bc with
        | MeasureByClass.TotalAcrossClasses x -> Some x
        | _ -> None

    let chooseForClass classId bc =
        match bc with
        | MeasureByClass.ForClass (c, x)  when c = classId -> Some x
        | _ -> None

   
    let fmap f bc = 
        match bc with
            | MeasureByClass.ForClass (id,x) ->  MeasureByClass.ForClass(id, f x)
            | MeasureByClass.TotalAcrossClasses x ->  MeasureByClass.TotalAcrossClasses (f x)

    let unWrap bc = 
        match bc with
            | MeasureByClass.ForClass (_,x) ->  x
            | MeasureByClass.TotalAcrossClasses x ->  x

    let unWrap' total forClass bc = 
        match bc with
            | MeasureByClass.ForClass (id,x) ->  forClass id x
            | MeasureByClass.TotalAcrossClasses x ->  total x

    let maxBy projection xs = xs |> List.map snd |> List.maxBy projection

module ForClass =

    let mkByClass (fc : MeasureForClass<'a>)  : MeasureByClass<'a> =
        let c, x = fc
        MeasureByClass.ForClass(c,x)

    let unWrap (fc : MeasureForClass<'a>) : 'a = snd fc

    let chooseForClass classId (fc : MeasureForClass<'a>) = 
        if fst fc = classId then Some (snd fc) else None

    let fMap (f : 'a -> 'b) (bc : MeasureForClass<'a>) : MeasureForClass<'b> =
        let c,x = bc
        (c,f x)
