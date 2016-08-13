namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Controls.Composite
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes

open System
open System.Windows
open System.Windows.Media

type ProductivityTrendData(providers : ProductivityChartProviders, formattedDateRange : string) =
    member x.Providers = providers
    member val FormattedDateRange = formattedDateRange with get, set

type ProductivityTrendsViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<ProductivityTrendData>(wds, false)
    let mutable data = ProductivityTrendData(ProductivityChartProviders(EmptyScalarSeriesDataProvider(),EmptyScalarSeriesDataProvider(),EmptyScalarSeriesDataProvider()), String.Empty)

    let readData (wd : WorkingData) = 
        async {
            let summaries = wd.storedSummaries.Value.daily
            

            let emptyDay = ClassificationByClassMeasurement.zeros wd.configuration.classification.filter ActivitySummary.Zero
            let dayCount = let ts = wd.maxmimumDate.Subtract(wd.minimumDate) in ts.TotalDays |> ceil |> int

            let convertDaily (xs : WithDate<MeasureByClass<ActivitySummary>> list) : MeasureByClass<ActivitySummary> list =
                let summs = xs |> List.map Dated.unWrap
                let zeroOnEmpty ys = 
                    assert (List.length ys <= 1)
                    if List.isEmpty ys then ActivitySummary.Zero else List.head ys
                ClassificationByClassMeasurement.filterMap wd.configuration.classification.filter zeroOnEmpty summs

            let convertWeekly (xs : WithDate<MeasureByClass<ActivitySummary>> list) : MeasureByClass<ActivitySummary> list =
                let summs = xs |> List.map Dated.unWrap
                ClassificationByClassMeasurement.filterMap wd.configuration.classification.filter Summaries.average summs

            let ts = 
                if dayCount <= 49 then
                    TimeSeries.mkSeries (Dated.dt >> TimeSeriesPeriods.mkDaily) convertDaily summaries
                        |> TimeSeries.regular (fun _ -> emptyDay) dayCount (TimeSeriesPeriods.mkDaily wd.maxmimumDate)
                        |> TimeSeries.opaque
                else
                    let weekCount = (float dayCount) / (7.0) |> floor |> int
                    TimeSeries.mkSeries (Dated.dt >> TimeSeriesPeriods.mkWeekly) convertWeekly summaries
                        |> TimeSeries.regular (fun _ -> emptyDay) weekCount (TimeSeriesPeriods.mkWeekly wd.maxmimumDate)
                        |> TimeSeries.opaque

            let max = TimeSeries.extractData (List.map ByClass.unWrap) ts |> List.map Summaries.max |> Summaries.max |> Humanize.roundUpSummary
            let maxWordCount = max.wordCount |> float
            let maxMinuteCount = max.minuteCount |> float
            let maxWpm = TimeSeries.extractData (List.map (ByClass.unWrap >> Summaries.wordsPerMinute)) ts |> List.map List.max |> List.max |> float

            
            let className x =
                ByClass.unWrap' (fun _ -> "Total") (fun c _ -> wd.configuration.classification.classNames c) x
            
            let wordEntry p (x : MeasureByClass<ActivitySummary>) : (float * obj) =
                let toWordCount (s : ActivitySummary) = s.wordCount
                let wordCount = float (ByClass.unWrap x |> toWordCount)
                (wordCount / maxWordCount), upcast (Theme.brushByClass wd.configuration.classification.colorMap x)

            let minuteEntry p (x : MeasureByClass<ActivitySummary>) : (float * obj) =
                let toMinute (s : ActivitySummary) = s.minuteCount
                let minuteCount = float (ByClass.unWrap x |> toMinute)
                (minuteCount / maxMinuteCount), upcast (Theme.brushByClass wd.configuration.classification.colorMap x)

            let wpmEntry p (x : MeasureByClass<ActivitySummary>) : (float * obj) =
                let wpmCount = float (ByClass.unWrap x |> Summaries.wordsPerMinute)
                let o = TimeSeriesPeriods.humanize p, ByClass.unWrap x, className x, Theme.brushByClass wd.configuration.classification.colorMap x
                (wpmCount / maxWpm), upcast o

            
            
            let axisLabels, words = TimeSeries.extractDataAndPeriodi (fun _ p s -> (TimeSeriesPeriods.humanize p, List.map (wordEntry p) s)) ts |> List.unzip
            let minutes = TimeSeries.extractDataAndPeriodi (fun _ p s -> (List.map (minuteEntry p) s)) ts
            let wpms = TimeSeries.extractDataAndPeriodi (fun _ p s -> (List.map (wpmEntry p) s)) ts

            let displayType = ScalarSeriesDisplayType.TrendLine

            let colors (o : obj) = 
                match o with
                 | :? (string * ActivitySummary * string * Brush) as x -> let _,_,_,b = x in b
                 | :? (Brush) as x -> x
                 | _ -> Theme.otherColors.back
            
            let providers =
               ProductivityChartProviders(
                                    ChartProviderHelper.scalarChart (Humanize.minutesFromStartOfDay (max.minuteCount / 2)) (Humanize.minutesFromStartOfDay max.minuteCount) axisLabels minutes displayType colors,
                                    ChartProviderHelper.scalarChart (string (max.wordCount / 2)) (string max.wordCount) axisLabels words displayType colors,
                                    ChartProviderHelper.scalarChart (string (int maxWpm / 2)) (string (int maxWpm)) axisLabels wpms displayType colors)
        
            
            return ProductivityTrendData(providers, DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter wd.minimumDate wd.maxmimumDate)
         }
    let showData d =
            data <- d
            vm.TriggerPropertyChanged "ProductivityChartProviders"
            vm.TriggerPropertyChanged "SubTitle"
    
    member x.ProductivityChartProviders = data.Providers
    member x.PeriodSelected p =
        data.FormattedDateRange <- p
        vm.TriggerPropertyChanged "SubTitle"
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Productivity Trends"
    override x.SubTitle = data.FormattedDateRange
    

type ProductivityTrendsWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<ProductivityTrendsViewModel, ProductivityTrendData>(wds, "ProductivityTrendsWidgetElement.xaml")

    let mutable vm : ProductivityTrendsViewModel option = None

    override x.CreateViewModel wds = 
        let v = ProductivityTrendsViewModel(wds)
        vm <- Some v
        v
    override x.InitialLoad uc =
        let charts = uc.FindName("ProductivityCharts") :?> ProductivityCharts
        charts.PeriodSelected.Add (fun p ->
                                        let v = Option.get vm
                                        v.PeriodSelected p)


module ProductivityTrendsWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast ProductivityTrendsWidgetElement(wds)

