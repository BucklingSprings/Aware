namespace BucklingSprings.Aware

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.DayStartAndEndTimes


[<RequireQualifiedAccess()>]
[<NoComparison()>]
[<NoEquality()>]
type StoredSummaries =
    {
        dayLengths: WithDate<DayLength> list
        hourly: WithDate<MeasureByClass<ActivitySummary>> list
        daily: WithDate<MeasureByClass<ActivitySummary>> list
        forHourOfWork: WithDate<MeasureByClass<ActivitySummary>> list
    }

module LazyTypedStoredSummariesReader =

    let read (sss : StoredSummary list) : Lazy<StoredSummaries> =
        lazy (
            let act, vers, daylengths = TypedStoredSummaries.read sss
            {
                StoredSummaries.dayLengths = daylengths
                StoredSummaries.daily = act |> TypedStoredSummaries.filterToPeriod SummaryTimePeriod.Day
                StoredSummaries.hourly = act |> TypedStoredSummaries.filterToPeriod SummaryTimePeriod.HourOfDay
                StoredSummaries.forHourOfWork = act |> TypedStoredSummaries.filterToPeriod SummaryTimePeriod.HourOfWork
            }
        )
        

