namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes

open System
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Collections.ObjectModel

type ProductivityDistributionEntryViewModel(c : string, b : Brush, stats : ActivitySummaryStatistics) =
    member x.ClassName  = c
    member x.ClassColor = b
    
    member x.Words = stats.wordStatistics.median
    member x.WordsLow = stats.wordStatistics.lowerQuartile
    member x.WordsHigh = stats.wordStatistics.upperQuartile

    member x.Minutes = stats.minuteStatistics.median
    member x.MinutesLow = stats.minuteStatistics.lowerQuartile
    member x.MinutesHigh = stats.minuteStatistics.upperQuartile

    member x.WordsPerMinute = stats.wordPerMinuteStatistics.median |> ceil |> int
    member x.WordsPerMinuteLow = stats.wordPerMinuteStatistics.lowerQuartile
    member x.WordsPerMinuteHigh = stats.wordPerMinuteStatistics.upperQuartile

type DesignTimeProductivityDistributionViewModel() =
    let details = ObservableCollection<ProductivityDistributionEntryViewModel>()

    let c1 = (Theme.customColors |> List.head).back
    let n1 = "Google Chrome"
    let stats1 = 
                {
                    ActivitySummaryStatistics.wordStatistics = StatisticalSummary.randomForDesignTime 5000
                    ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.randomForDesignTime 400
                    ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.randomForDesignTime 17
                }

    let c2 = (Theme.customColors |> List.rev |> List.head).back
    let n2 = "Internet Explorer"
    let stats2 = 
                {
                    ActivitySummaryStatistics.wordStatistics = StatisticalSummary.randomForDesignTime 6000
                    ActivitySummaryStatistics.minuteStatistics = StatisticalSummary.randomForDesignTime 600
                    ActivitySummaryStatistics.wordPerMinuteStatistics = StatisticalSummary.randomForDesignTime 12
                }
    do
        details.Add(ProductivityDistributionEntryViewModel(n1,c1, stats1))
        details.Add(ProductivityDistributionEntryViewModel(n2,c2, stats2))
    member x.Details = details

type ProductivityDistributionViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<ProductivityDistributionEntryViewModel list * string>(wds, false)

    let details = ObservableCollection<ProductivityDistributionEntryViewModel>()
    let mutable subTitle = String.Empty
    
    let readData (wd : WorkingData) = 
        async {
            let daily : WithDate<MeasureByClass<ActivitySummary>> list = wd.storedSummaries.Value.daily
            
            let summarize (xs : WithDate<MeasureByClass<ActivitySummary>> list) : MeasureByClass<ActivitySummaryStatistics> list =
                ClassificationByClassMeasurement.filterMap wd.configuration.classification.filter Summaries.summarize (xs |> List.map Dated.unWrap)

            let toEntry (x : MeasureByClass<ActivitySummaryStatistics>) : ProductivityDistributionEntryViewModel  =
                let b = Theme.brushByClass wd.configuration.classification.colorMap x
                let className = ByClass.unWrap' (fun _ -> "Total") (fun c _ -> wd.configuration.classification.classNames c) x
                ProductivityDistributionEntryViewModel(className, b, ByClass.unWrap x)

            return (daily |> summarize |> List.map toEntry, DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter wd.minimumDate wd.maxmimumDate)
         }
    let showData d =
        let entries, dateRange = d
        details.Clear()
        entries |> List.iter (fun e -> details.Add(e))
        subTitle <- dateRange
        vm.TriggerPropertyChanged "SubTitle"
    
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.SubTitle = subTitle
    override x.Title = "Typical Usage"
    member x.Details = details
    

type ProductivityDistributionWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<ProductivityDistributionViewModel, ProductivityDistributionEntryViewModel list * string>(wds, "ProductivityDistributionWidgetElement.xaml")
    override x.CreateViewModel wds = ProductivityDistributionViewModel(wds)

    
module ProductivityDistributionWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast ProductivityDistributionWidgetElement(wds)
