namespace BucklingSprings.Aware.Controls.Charts


open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Common.UserConfiguration

open System
open System.Windows.Media

[<NoComparison()>]
type DayMap = TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, TimeSeriesPeriodTimeOfDay, MeasureForClass<ActivitySummary>>


[<NoComparison()>]
type TimeMap = TimeSeriesPhantom<TimeSeriesOrderedByPeriodDesc, TimeSeriesRegular, TimeSeriesCompletenessUnknown, TimeSeriesPeriodDaily, DayMap>


type ITimeMapDataProvider =
    abstract TimeMap : TimeMap
    abstract BrushesForClass : ClassIdentifier -> AssignedBrushes

type EmptyTimeMapDataProvider() =
    interface ITimeMapDataProvider with
        member tm.BrushesForClass _ = {fore = Brushes.White :> Brush; back = Brushes.Red :> Brush}
        member tm.TimeMap = 
            let now = System.DateTimeOffset.Now
            let last7Days = [for i in 0 .. 6 -> now.AddDays(-1.0 * (float i)) ]
            let emptyDay : DayMap = TimeSeries.mkEmpty |> TimeSeries.sort
            let dataForDay _ = emptyDay
            let ts = TimeSeries.mkSeries TimeSeriesPeriods.mkDaily dataForDay last7Days
            let now = System.DateTimeOffset.Now
            let maxDt = TimeSeriesPeriods.mkDaily now
            let minDt = TimeSeriesPeriods.mkDaily (now.AddDays(-7.0))
            ts |> TimeSeries.sort |> TimeSeries.regular dataForDay 7 maxDt |> TimeSeries.reverse
            


