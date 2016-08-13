namespace BucklingSprings.Aware

open System

module LazyServices =

    let focusSessionProgressReportingService = lazy (FocusSessionProgressReportingService())

