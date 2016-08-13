namespace BucklingSprings.Aware.Controls.Composite

open System
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Collections.ObjectModel

open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Common.UserConfiguration

[<NoComparison()>]
[<RequireQualifiedAccess()>]
type ProductivityDistributionProviders
            (
                minuteProvider : IBoxPlotDataProvider,
                wordProvider : IBoxPlotDataProvider,
                wpmProvider : IBoxPlotDataProvider,
                classDetails : (string * Brush) list
            ) =
    member val MinutesWorkedDistributionDataProvider = minuteProvider with get
    member val WordCountDistributionDataProvider = wordProvider with get
    member val WordsPerMinuteDistributionDataProvider = wpmProvider with get
    member val ClassDetails = classDetails


type IProductivityDistributionRegionDetail =
    abstract FiveNumberSummary : FiveNumberSummary<float>


type DistributionDetail(detail : ActivitySummaryStatistics, className, classBrush) =
    member x.Words = int detail.wordStatistics.median
    member x.WordsLow = int detail.wordStatistics.lowerQuartile
    member x.WordsHigh = int detail.wordStatistics.upperQuartile
    member x.Minutes = int detail.minuteStatistics.median
    member x.MinutesLow = int detail.minuteStatistics.lowerQuartile
    member x.MinutesHigh = int detail.minuteStatistics.upperQuartile
    member x.WordsPerMinute = int detail.wordPerMinuteStatistics.median
    member x.WordsPerMinuteLow = int detail.wordPerMinuteStatistics.lowerQuartile
    member x.WordsPerMinuteHigh = int detail.wordPerMinuteStatistics.upperQuartile
    member x.ClassName = className
    member x.ClassColor = classBrush
    

type DesignTimeProductivtyDistributionControlViewModel() =
    let details = ObservableCollection<DistributionDetail>()
    let s1= {
                ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.randomForDesignTime 10
                ActivitySummaryStatistics.wordStatistics = StatisticalSummary.randomForDesignTime 1000
                ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.randomForDesignTime 120
            }
    let s2= {
                ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.randomForDesignTime 12
                ActivitySummaryStatistics.wordStatistics = StatisticalSummary.randomForDesignTime 1200
                ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.randomForDesignTime 200
            }
    let c1 = (Theme.customColors |> List.head ).back
    let c2 = Theme.awareBrush

    let classDetails = [("Class 1", c1); ("Class 2", upcast c2)]
    do
        details.Add(DistributionDetail(s1, "Class 1", c1))
        details.Add(DistributionDetail(s2, "Class 2", c2))
        
    member x.ProductivtyDistributionControlProviders = ProductivityDistributionProviders(DesigntimeBoxPlotDataProvider(),DesigntimeBoxPlotDataProvider(),DesigntimeBoxPlotDataProvider(), classDetails)
    member x.Details = details


type ProductivtyDistributionControl() as uc =
    inherit UserControl()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ProductivtyDistributionControl.xaml", UriKind.Relative)) :?> UserControl

    let minutesBoxPlot = content.FindName("MinuteDistributionBoxPlot") :?> BoxPlotControl
    let wordsBoxPlot = content.FindName("WordDistributionBoxPlot") :?> BoxPlotControl
    let wordsPerMinuteBoxPlot = content.FindName("WordsPerMinuteDistributionBoxPlot") :?> BoxPlotControl

    let wordDetails = content.FindName("WordDetails") :?> ItemsControl
    let minuteDetails = content.FindName("MinuteDetails") :?> ItemsControl
    let wordsPerMinuteDetails = content.FindName("WordPerMinuteDetails") :?> ItemsControl
    let details = ObservableCollection<DistributionDetail>()

    
    let extractDistributions (e : BoxPlotRegionHoverEventArgs) =
        let extractFn (o : obj) = 
            match o with
            | :? (String * Brush * ActivitySummaryStatistics) as d -> Some d
            | _ -> None
        e.BoxPlotPoints.values |> List.map snd |> List.choose extractFn

    let showDetail e =
        details.Clear()
        let stats = extractDistributions e
        stats |> List.iter (fun (name, brush, stats) -> details.Add(DistributionDetail(stats, name, brush)))
        

    do

        minutesBoxPlot.PointHoverEnter.Add(fun e ->
                                            if e.UserAction then
                                                wordsBoxPlot.SelectPointByIndex e.PointIndex
                                                wordsPerMinuteBoxPlot.SelectPointByIndex e.PointIndex
                                                showDetail e)

        wordsBoxPlot.PointHoverEnter.Add(fun e ->
                                            if e.UserAction then
                                                minutesBoxPlot.SelectPointByIndex e.PointIndex
                                                wordsPerMinuteBoxPlot.SelectPointByIndex e.PointIndex
                                                showDetail e)

        wordsPerMinuteBoxPlot.PointHoverEnter.Add(fun e ->
                                            if e.UserAction then
                                                minutesBoxPlot.SelectPointByIndex e.PointIndex
                                                wordsBoxPlot.SelectPointByIndex e.PointIndex
                                                showDetail e)

        wordDetails.DataContext <- uc
        minuteDetails.DataContext <- uc
        wordsPerMinuteDetails.DataContext <- uc

        uc.Content <- content
    
    static let emptyProvider = EmptyBoxPlotDataProvider()
    static let ProductivtyDistributionControlProvidersProperty =
                DependencyProperty.Register(
                                                "ProductivtyDistributionControlProviders",
                                                typeof<ProductivityDistributionProviders>,
                                                typeof<ProductivtyDistributionControl>,
                                                new PropertyMetadata(
                                                    ProductivityDistributionProviders(emptyProvider, emptyProvider, emptyProvider,[("Total", upcast Theme.awareBrush)]),
                                                    new PropertyChangedCallback(ProductivtyDistributionControl.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? ProductivtyDistributionControl as t -> t.Redraw()
        | _ -> ()

    member x.Redraw () =
        let p : ProductivityDistributionProviders = x.ProductivtyDistributionControlProviders
        details.Clear()
        p.ClassDetails |> List.iter (fun (n,b) -> details.Add(DistributionDetail(ActivitySummaryStatistics.SomeNegativeValue,n,b)))
        
    member x.ProductivtyDistributionControlProviders
        with get() = x.GetValue(ProductivtyDistributionControlProvidersProperty) :?> ProductivityDistributionProviders
        and  set(v : ProductivityDistributionProviders) = 
            
            x.SetValue(ProductivtyDistributionControlProvidersProperty, v)
    member x.Details = details
