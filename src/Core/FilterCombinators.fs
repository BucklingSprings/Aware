namespace BucklingSprings.Aware.Core

module Filters =

    let fAnd (e : 'a -> bool) (f : 'a -> bool) : 'a -> bool =
        fun x -> if e x then f x else false

    let fOr (e : 'a -> bool) (f : 'a -> bool) : 'a -> bool =
        fun x -> if e x then true else f x

    let fChoose (f : 'a -> bool) (e : 'a -> 'b) : 'a -> 'b option =
        fun x -> if f x then Some (e x) else None