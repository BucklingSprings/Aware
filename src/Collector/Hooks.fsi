namespace BucklingSprings.Aware.Collector

open BucklingSprings.Aware.Core.Models

module Hooks =
    val setInputHooks : unit -> unit
    val getAndClearActivity : unit -> InputActivity