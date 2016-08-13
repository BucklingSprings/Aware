namespace BucklingSprings.Aware.Controls.Composite

open System.Windows.Media

open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.TimeSeriesPhantom

open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls.Charts



type ProductivityDistributionDataObject (values : MeasureByClass<FiveNumberSummary<float>>) =
    interface IProductivityDistributionRegionDetail with
        override x.FiveNumberSummary = ByClass.unWrap values

module TimeSeriesProductivityDistributionCharts =
    let providers 
            (config : UserGlobalConfiguration) 
            (ts : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, 'p, MeasureByClass<ActivitySummaryStatistics> list>) 
                : ProductivityDistributionProviders =
        
        let max = 
            ts
                |> TimeSeries.extractData (List.map (ByClass.unWrap >> Summaries.maxFromStats) >> Summaries.max)
                |> Summaries.max
                |> Humanize.roundUpSummary

        let maxWpm = 
            ts
                |> TimeSeries.extractData (List.map (ByClass.unWrap >> Summaries.wpmMax) >> List.max)
                |> List.max

        let providerData (x: MeasureByClass<ActivitySummaryStatistics>) : obj =
            let stats = ByClass.unWrap x
            let classId = ByClass.chooseTotal
            let brush = Theme.brushByClass config.classification.colorMap x
            let className = ByClass.unWrap' (fun _ -> "Total") (fun c _ -> config.classification.classNames c) x
            upcast (className, brush, stats)

        let asWordData (x : MeasureByClass<ActivitySummaryStatistics>) : (FiveNumberSummary<float> * obj) =
            let stats = ByClass.unWrap x
            (StatisticalSummary.normalize stats.wordStatistics (float max.wordCount), providerData x)

        let asMinuteData (x : MeasureByClass<ActivitySummaryStatistics>) : (FiveNumberSummary<float> * obj) =
            let stats = ByClass.unWrap x
            (StatisticalSummary.normalize stats.minuteStatistics (float max.minuteCount), providerData x)

        let asWpmData (x : MeasureByClass<ActivitySummaryStatistics>) : (FiveNumberSummary<float> * obj) =
            let stats = ByClass.unWrap x
            (StatisticalSummary.normalize stats.wordPerMinuteStatistics (float maxWpm), providerData x)

        let colorMap (o : obj) : Brush =
            match o with
            | :? (string * Brush * ActivitySummaryStatistics)  as t -> (fun (_,b,_) -> b) t
            | _ -> Theme.otherColors.back

        let xLabels = TimeSeries.extractPeriods (TimeSeriesPeriods.humanize) ts

        let classes : (string * Brush) list = ClassificationClassFilterUtils.map ("Total", upcast Theme.awareBrush) (fun cls -> (config.classification.classNames (ClassIdentifier cls.id), (config.classification.colorMap (ClassIdentifier cls.id)).back)) config.classification.filter

        
        ProductivityDistributionProviders
            (
                FiveNumberSummaryBoxPlotDataProvider(ts |> TimeSeries.extractData (List.map asMinuteData), colorMap, Humanize.minutesFromStartOfDay (max.minuteCount/2), Humanize.minutesFromStartOfDay max.minuteCount, xLabels),
                FiveNumberSummaryBoxPlotDataProvider(ts |> TimeSeries.extractData (List.map asWordData), colorMap, string (max.wordCount/2), string max.wordCount, xLabels),
                FiveNumberSummaryBoxPlotDataProvider(ts |> TimeSeries.extractData (List.map asWpmData), colorMap, string (maxWpm/2), string maxWpm, xLabels),
                classes
            )
