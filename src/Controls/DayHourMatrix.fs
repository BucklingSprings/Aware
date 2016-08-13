namespace BucklingSprings.Aware.Controls.Charts

open System
open System.Windows.Media

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Common.Themes

[<NoComparison()>]
[<RequireQualifiedAccess()>]
type DayHourMatrixHourData = {value : float; data : obj ; brush : Brush}


type DayHourMatrixDayData = TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, TimeSeriesPeriodHourOfDay, DayHourMatrixHourData list>

type IDayHourMatrixProvider =
    abstract DayHourMatrix : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, TimeSeriesPeriodDayOfWeek, DayHourMatrixDayData>


type EmptyDayHourMatrixProvider() =
    let zero = {DayHourMatrixHourData.value = 0.0; DayHourMatrixHourData.data = () :> obj; DayHourMatrixHourData.brush = Theme.awareBrush }
    let hours = TimeSeriesPhantom.TimeSeries.mkEmpty<TimeSeriesPeriodHourOfDay, DayHourMatrixHourData list> |> TimeSeries.complete (fun _ -> [zero])
    let days = TimeSeriesPhantom.TimeSeries.mkEmpty<TimeSeriesPeriodDayOfWeek, DayHourMatrixDayData> |> TimeSeries.complete (fun _ -> hours)

    interface IDayHourMatrixProvider with
        member x.DayHourMatrix = days

type DesignTimeDayHourMatrixDataProvider() =
    let c1 = (Theme.customColors |> List.head).back
    let c2 = (Theme.customColors |> List.tail |> List.head).back
    let rnd = Random()
    let value hour =
        if hour < 5 || hour > 21 then
            0.0
        elif hour < 8 || hour > 19 then
            rnd.NextDouble() / 2.0
        else
            rnd.NextDouble()
            
    let point p b = {DayHourMatrixHourData.value = value (TimeSeriesPeriods.hourOfDay p); DayHourMatrixHourData.data = () :> obj; DayHourMatrixHourData.brush = b }
    let hours = TimeSeriesPhantom.TimeSeries.mkEmpty<TimeSeriesPeriodHourOfDay, DayHourMatrixHourData list> |> TimeSeries.complete (fun p -> [point p c1; point p c2])
    let days = TimeSeriesPhantom.TimeSeries.mkEmpty<TimeSeriesPeriodDayOfWeek, DayHourMatrixDayData> |> TimeSeries.complete (fun _ -> hours)
    interface IDayHourMatrixProvider with
        member x.DayHourMatrix = days