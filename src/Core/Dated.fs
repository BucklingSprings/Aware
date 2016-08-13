namespace BucklingSprings.Aware.Core

open System
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

[<RequireQualifiedAccess()>]
type WithDate<'a> = DateTimeOffset * 'a

module Dated = 
    
        
    let dt : (WithDate<'a> -> DateTimeOffset) = fst
    let mkDated (x : 'a) (d : DateTimeOffset) = (d, x)
    let unWrap : WithDate<'a> -> 'a = snd
    let wrap (x : 'a) (f : 'a -> DateTimeOffset) = (f x, x)

    let fMap (f : 'a -> 'b) (d : WithDate<'a>) : WithDate<'b> =
        let d, x = d
        d, f x

    let filterForDay (d : System.DateTimeOffset) (x : WithDate<'a>) : bool =
        let dt = fst x
        dt >= d.StartOfDay && dt <= d.EndOfDay

