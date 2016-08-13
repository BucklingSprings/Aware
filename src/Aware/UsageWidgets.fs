namespace BucklingSprings.Aware.Widgets

open System
open System.Windows
open System.Windows.Media
open System.Windows.Controls

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Summaries

type UsageDuration =
    | UsageRange
    | UsageDaily

type UsageMetricData(prov : ICircleChartDataProvider, total : int) =
    member x.Provider = prov
    member x.Total = total

type UsageData(wordData : UsageMetricData, minuteData : UsageMetricData, subTitle : string, classNameMap : ClassIdentifier -> string) =
    member x.Words = wordData
    member x.Minutes = minuteData
    member x.subTitle = subTitle
    member x.ClassNameMap = classNameMap

type UsageMinutesHoverData(minutes : int, className : string, classColor : Brush) =
    member x.Minutes = minutes
    member x.ClassName = className
    member x.ClassColor = classColor

type UsageWordsHoverData(words : int) =
    member x.Words = words

type DesignTimeUsageViewModel() =
    let c1 = (Theme.customColors |> List.head).back
    let c2 = (Theme.customColors |> List.tail |> List.head).back
    member x.MinutesDataProvider = EmptyCircleChartDataProvider() :> ICircleChartDataProvider
    member x.WordsDataProvider = EmptyCircleChartDataProvider() :> ICircleChartDataProvider
    member x.UsageMinutesHoverData = UsageMinutesHoverData(2628000, "Internet Explorer", c1)
    member x.UsageWordsHoverData = UsageWordsHoverData(1200)

type UsageViewModel(wds : WorkingDataService, usageDuration : UsageDuration) as vm =
    inherit WidgetViewModelBase<UsageData>(wds, true)
    let empty = UsageMetricData(EmptyCircleChartDataProvider() :> ICircleChartDataProvider, 0)
    let mutable usageData : UsageData = UsageData(empty, empty, System.String.Empty, fun _ -> String.Empty)
    let mutable usageMinutesHoverData : UsageMinutesHoverData = UsageMinutesHoverData(0, String.Empty, Theme.offColors.fore)
    let mutable usageWordsHoverData : UsageWordsHoverData = UsageWordsHoverData(0)
    
    let readData (wd : WorkingData) = 
        async {
            let classes =  ClassificationClassFilterUtils.selectedClassesOrAll wd.configuration.classification
            let totalsForRange (wd : WorkingData) : ((ClassIdentifier * ActivitySummary) list) =
                let summs = wd.storedSummaries.Value.daily |> List.map Dated.unWrap
                ClassificationByClassMeasurement.filterMapForClasses classes List.sum summs
            let totalsForDay (wd : WorkingData) : ((ClassIdentifier * ActivitySummary) list) =
                let samplesForDay = 
                    wd.lastNDaySamplesForCurrentClassifier
                        |> List.filter (Dated.filterForDay (DataDateRangeFilterUtils.endDt wd.configuration.dateRangeFilter))
                        |> List.map Dated.unWrap
                ClassificationForClassMeasurement.filterMapForClasses classes List.sum samplesForDay
        
        
            let totals = 
                match usageDuration with
                | UsageDuration.UsageDaily -> totalsForDay wd
                | UsageDuration.UsageRange -> totalsForRange wd
                
            let total = totals |> List.sumBy snd

            let words = totals |> List.map (fun (c, s) ->  (s.wordCount, c)) |> List.sortBy fst
            let minutes = totals |> List.map (fun (c, s) -> (s.minuteCount, c)) |> List.sortBy fst
            let colorMap = fun x -> (wd.configuration.classification.colorMap x).back
            let wordData = UsageMetricData(CirclePlotPercentageDataProvider(words, colorMap), total.wordCount)
            let minuteData = UsageMetricData(CirclePlotPercentageDataProvider(minutes, colorMap), total.minuteCount)

            let subTitle = 
                match usageDuration with
                | UsageDuration.UsageDaily -> DataDateRangeFilterUtils.formattedEndDt wd.configuration.dateRangeFilter
                | UsageDuration.UsageRange -> DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter wd.minimumDate wd.maxmimumDate
            
            return UsageData(wordData, minuteData, subTitle, wd.configuration.classification.classNames)
            
         }
    let showData prod =
            usageData <- prod
            usageMinutesHoverData <- UsageMinutesHoverData(usageData.Minutes.Total, "Total", Theme.totalColors.back)
            usageWordsHoverData <- UsageWordsHoverData(usageData.Words.Total)
            vm.TriggerPropertyChanged "MinutesDataProvider"
            vm.TriggerPropertyChanged "WordsDataProvider"
            vm.TriggerPropertyChanged "UsageMinutesHoverData"
            vm.TriggerPropertyChanged "UsageWordsHoverData"
            vm.TriggerPropertyChanged "SubTitle"
    
    member x.MinutesDataProvider = usageData.Minutes.Provider
    member x.WordsDataProvider = usageData.Words.Provider
    member x.UsageMinutesHoverData = usageMinutesHoverData
    member x.UsageWordsHoverData = usageWordsHoverData
    member x.ShowWordCount words = 
        usageWordsHoverData <- UsageWordsHoverData(words)
        x.TriggerPropertyChanged "UsageWordsHoverData"
    member x.ShowMinuteDetails minutes brush (clsId : ClassIdentifier) = 
        usageMinutesHoverData <- UsageMinutesHoverData(minutes, usageData.ClassNameMap clsId, brush)
        x.TriggerPropertyChanged "UsageMinutesHoverData"

    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = 
        match usageDuration with
        | UsageDuration.UsageDaily -> "Usage Daily"
        | UsageDuration.UsageRange -> "Usage Totals"

    override x.SubTitle = usageData.subTitle

type UsageWidgetElement(wds, usageDuration) =
    inherit StandardWidgetElementBase<UsageViewModel, UsageData>(wds, "UsageWidgetElement.xaml")

    let mutable viewModel : UsageViewModel option = None

    let extractPercentage (o : obj) =
        match o with
            | :? CircleChartPercentageDataPoint<ClassIdentifier> as p -> Some p
            | _ -> None


    let sync (e : CircleSegmentHoverEventArgs) (synced : CircleChartControl) = 
        let pO = extractPercentage e.Data
        if Option.isSome pO then
            let percetagePoint = Option.get pO
            
            let predicate o =
                let pO' = extractPercentage o
                if Option.isSome pO' then
                    let percetagePoint' = Option.get pO'
                    percetagePoint'.Data = percetagePoint.Data
                else
                    false
            if e.UserAction then
                synced.SelectByPredicate predicate

    let showWordDetails (e : CircleSegmentHoverEventArgs) =
        let pO = extractPercentage e.Data
        if Option.isSome pO then
            let percetagePoint = Option.get pO
            let vm = Option.get viewModel
            vm.ShowWordCount(percetagePoint.RawValue)

    let showMinuteDetails (e : CircleSegmentHoverEventArgs) =
        let pO = extractPercentage e.Data
        if Option.isSome pO then
            let percetagePoint = Option.get pO
            let vm = Option.get viewModel
            vm.ShowMinuteDetails percetagePoint.RawValue percetagePoint.Brush percetagePoint.Data

    override x.CreateViewModel wds = 
        let vm = UsageViewModel(wds, usageDuration)
        viewModel <- Some vm
        vm
    override x.InitialLoad content =
        let minutesCircleChart = content.FindName("MinutesCircleChart") :?> CircleChartControl
        let wordsCircleChart = content.FindName("WordsCircleChart") :?> CircleChartControl
        minutesCircleChart.PointHoverEnter.Add(fun e -> 
                                                        showMinuteDetails e
                                                        sync e wordsCircleChart)
        wordsCircleChart.PointHoverEnter.Add(fun e -> 
                                                    showWordDetails e
                                                    sync e minutesCircleChart)
    
module TotalUsageWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast UsageWidgetElement(wds, UsageDuration.UsageRange)

module DailyUsageWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast UsageWidgetElement(wds, UsageDuration.UsageDaily)


