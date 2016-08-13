namespace BucklingSprings.Aware.Common.Totals

open BucklingSprings.Aware.Core.Diagnostics

[<RequireQualifiedAccess()>]
[<NoComparison()>]
[<NoEquality()>]
type TotalBy<'G, 'I> = TotalBy of 'G  * ('I -> bool)

module SeriesTotaler =
 
     let inline totals (totalBy : TotalBy<'G, 'A> list) (projection : 'A -> ^T ) (values : 'A list) : ('G *'T) list = 
        let assignToTotals (TotalBy.TotalBy (group, chooser)) =
            let includedInGroup = values |> List.filter chooser
            let total = List.sumBy projection includedInGroup
            (group, total)
        totalBy |> List.map assignToTotals
