#nowarn "52"

namespace BucklingSprings.Aware.Core.Utils

open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

module Dates = 

    let today () = System.DateTimeOffset.Now.StartOfDay

    let daysAgo n = System.DateTimeOffset.Now.AddDays(-1.0 * (float n)).StartOfDay

    let yesterday () = daysAgo 1

