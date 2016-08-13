namespace BucklingSprings.Aware.Threading

open System.Threading

module UIContext =

    let mutable ctx : SynchronizationContext = null

    let initialize () =
        ctx <- SynchronizationContext.Current

    let instance () = ctx

