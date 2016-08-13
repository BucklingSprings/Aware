namespace BucklingSprings.Aware.Controls.Composite

open System
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Collections.ObjectModel

open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core


type ProductivityChartProviders(minuteChartProvider : IScalarSeriesDataProvider, wordChartProvider : IScalarSeriesDataProvider, wordsPerMinuteChartProvider : IScalarSeriesDataProvider) =
    member val MinuteChartProvider = minuteChartProvider with get
    member val WordChartProvider = wordChartProvider with get
    member val WordPerMinuteChartProvider = wordsPerMinuteChartProvider with get

[<AllowNullLiteral()>]
type ProductivityChartPointDetail(hours : int, minutes : int, words : int, wpm : int) =
    member x.Hours = hours
    member x.Minutes = minutes
    member x.Words = words
    member x.WordsPerMinute = wpm


type ProductivityChartClassDetail(name : string, classColor : Brush, pointDetail : ProductivityChartPointDetail) =
    member x.ClassName = name
    member x.ClassColor = classColor
    member x.PointDetail = pointDetail


type DesignTimeProductivityChartsViewModel() =
    let details = ObservableCollection<ProductivityChartClassDetail>()
    do
        let c1 = (Theme.customColors |> List.head).back
        let c2 = (Theme.customColors |> List.tail |> List.head).back
        details.Add(ProductivityChartClassDetail("Design Time 1", c1, ProductivityChartPointDetail(0, 23, 10, 8)))
        details.Add(ProductivityChartClassDetail("Design Time 2", c2, null))
    member val ProductivityChartProviders = ProductivityChartProviders(DesignTimerTrendDataProvider(), DesignTimerTrendDataProvider(), DesignTimerTrendDataProvider()) with get, set
    member x.TrendDetails = details
        

type ProductivityCharts() as uc =
    inherit UserControl()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ProductivityCharts.xaml", UriKind.Relative)) :?> UserControl  

    let minuteChart = content.FindName("MinuteTrendsChart") :?> ScalarSeriesChartControl
    let wordChart = content.FindName("WordTrendsChart") :?> ScalarSeriesChartControl
    let wordsPerMinuteChart = content.FindName("WordsPerMinuteTrendChart") :?> ScalarSeriesChartControl
    let details = ObservableCollection<ProductivityChartClassDetail>()

    
    let minuteDetails = content.FindName("MinuteDetails") :?> ItemsControl
    let wordDetails = content.FindName("WordDetails") :?> ItemsControl
    let wordsPerMinuteDetails = content.FindName("WordsPerMinuteDetails") :?> ItemsControl
    let periodSelected = new Event<string>()

    
    let dataToPointDetails (x : float * obj) : (ActivitySummary * string * Brush) option =
        let v, o = x
        match o with
        | :? (string * ActivitySummary * string * Brush) as pt -> 
                let p, total, clasName, brush = pt
                let providers : ProductivityChartProviders = uc.ProductivityChartProviders
                Some (total, clasName, brush)
        | _ -> None

    let dataToPeriod (xs : (float * obj) list) : string option =
        if List.isEmpty xs then
            None
        else
            let x = List.head xs
            let v, o = x
            match o with
            | :? (string * ActivitySummary * string * Brush) as pt -> 
                    let p, _, _, _ = pt
                    Some p
            | _ -> None

    let activitySummaryToPointDetail (a : ActivitySummary) : ProductivityChartPointDetail =
        let ts = TimeSpan.FromMinutes(float a.minuteCount)
        ProductivityChartPointDetail(ts.Hours, ts.Minutes, a.wordCount, Summaries.wordsPerMinute a)

    let activityTotalToChartDetail (ignoreValues : bool) (a : ActivitySummary * string * Brush)  : ProductivityChartClassDetail =
        let summ, className, brush = a
        ProductivityChartClassDetail(className, brush, if ignoreValues then null else activitySummaryToPointDetail summ)
        


    do
        uc.Content <- content
        content.DataContext <- uc
        minuteChart.PointHoverEnter.Add
            (fun e
                ->
                    if e.UserAction then
                        wordChart.SelectPointByIndex e.PointIndex
                        wordsPerMinuteChart.SelectPointByIndex e.PointIndex
                       )
        wordChart.PointHoverEnter.Add
            (fun e
                -> 
                    if e.UserAction then 
                        minuteChart.SelectPointByIndex e.PointIndex
                        wordsPerMinuteChart.SelectPointByIndex e.PointIndex
                        )
        wordsPerMinuteChart.PointHoverEnter.Add
            (fun e
                -> 
                    let points = e.ScalarValuePoints.values
                    details.Clear()
                    let timePeriod = dataToPeriod points

                    if Option.isSome timePeriod then
                        periodSelected.Trigger(Option.get timePeriod)

                    if not (List.isEmpty points) then
                            let values = points |> List.choose dataToPointDetails
                            values |> List.map (activityTotalToChartDetail false) |> List.iter (fun a -> details.Add(a))
                    if e.UserAction then 
                        minuteChart.SelectPointByIndex e.PointIndex
                        wordChart.SelectPointByIndex e.PointIndex
                        )
        wordsPerMinuteChart.Redrawn.Add(fun e
                                            ->
                                                details.Clear()
                                                let providers : ProductivityChartProviders = uc.ProductivityChartProviders
                                                let wpmPoints = providers.WordPerMinuteChartProvider.ChartData.points
                                                if not (List.isEmpty wpmPoints) then
                                                    let firstValues = (wpmPoints |> List.head).values
                                                    if not (List.isEmpty firstValues) then
                                                        let values = firstValues |> List.choose dataToPointDetails
                                                        values |> List.map (activityTotalToChartDetail true) |> List.iter (fun a -> details.Add(a))
                                                )
    static let ProductivityChartProvidersProperty =
                DependencyProperty.Register(
                                                "ProductivityChartProviders",
                                                typeof<ProductivityChartProviders>,
                                                typeof<ProductivityCharts>,
                                                new PropertyMetadata(ProductivityChartProviders(EmptyScalarSeriesDataProvider(), EmptyScalarSeriesDataProvider(), EmptyScalarSeriesDataProvider())))
    member x.TrendDetails = details
    member x.PeriodSelected = periodSelected.Publish
    member x.ProductivityChartProviders
        with get() = x.GetValue(ProductivityChartProvidersProperty) :?> ProductivityChartProviders
        and  set(v : ProductivityChartProviders) = x.SetValue(ProductivityChartProvidersProperty, v)
