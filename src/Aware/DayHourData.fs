namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Common.Themes

open BucklingSprings.Aware.Core.Models 
open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Measurement

open BucklingSprings.Aware.Common.UserConfiguration

open BucklingSprings.Aware.Controls.Charts


module DayHourData =

    
    type DayHourHourlyData = TimeSeriesPhantom.TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, TimeSeriesPeriodHourOfDay, MeasureByClass<ActivitySummaryStatistics> list>
    type DayHourDailyDataData = TimeSeriesPhantom.TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, TimeSeriesPeriodDayOfWeek, DayHourHourlyData>

    let dailyDataTo (wd : WorkingData) : int * ActivitySummary * DayHourDailyDataData =
        let hourly : WithDate<MeasureByClass<ActivitySummary>> list = wd.storedSummaries.Value.hourly
        let config = wd.configuration
        let cf = wd.configuration.classification.filter
        let zeroHour = ClassificationByClassMeasurement.zeros cf ActivitySummaryStatistics.Zero
        let zeroDay : DayHourHourlyData = TimeSeriesPhantom.TimeSeries.mkEmpty |> TimeSeries.complete (fun _ -> zeroHour)

        let summarize (xs : WithDate<MeasureByClass<ActivitySummary>> list) : MeasureByClass<ActivitySummaryStatistics> list =
            ClassificationByClassMeasurement.filterMap cf Summaries.summarize (xs |> List.map Dated.unWrap)
        
        let mkHourlySeries xs : DayHourHourlyData = 
            xs
                |> TimeSeriesPhantom.TimeSeries.mkSeries  (Dated.dt >> TimeSeriesPhantom.TimeSeriesPeriods.mkHourOfDay) summarize
                |> TimeSeriesPhantom.TimeSeries.complete (fun _ -> zeroHour)

        let ts = hourly
                    |> TimeSeriesPhantom.TimeSeries.mkSeries (Dated.dt >> TimeSeriesPhantom.TimeSeriesPeriods.mkDayOfWeek) mkHourlySeries
                    |> TimeSeriesPhantom.TimeSeries.complete (fun _ -> zeroDay)

        let max = 
            ts
                |> TimeSeries.extractData (TimeSeries.extractData (List.map (ByClass.unWrap >> Summaries.medians) >> Summaries.max))
                |> List.map Summaries.max
                |> Summaries.max

        let maxWpm = 
            ts
                |> TimeSeries.extractData (TimeSeries.extractData (List.map (ByClass.unWrap >> Summaries.wpmMedian) >> List.max))
                |> List.map List.max
                |> List.max


       
        maxWpm, max, ts

