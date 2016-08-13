namespace BucklingSprings.Aware.Controls.Media.Effects

open BucklingSprings.Aware.Core.Diagnostics

open System.Windows.Media
open System.Windows.Media.Effects


module Effect =

    let addEffect (v : Visual) (e : Effect) =
        match v with
        | :? DrawingVisual as dv -> dv.Effect <- e
        | _ -> assert false

    let removeEffect (v : Visual) =
        match v with
        | :? DrawingVisual as dv -> dv.Effect <- null
        | _ -> assert false

    let showVisual (v : Visual) =
        match v with
        | :? DrawingVisual as dv -> dv.Opacity <- 1.0
        | _ -> assert false

    let hideVisual (v : Visual) =
        match v with
        | :? DrawingVisual as dv -> dv.Opacity <- 0.0
        | _ -> assert false