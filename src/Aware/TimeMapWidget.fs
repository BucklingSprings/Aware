#nowarn "52"

namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Models


open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Focus
open BucklingSprings.Aware.Common.Themes

open BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware

open System
open System.Threading
open System.ComponentModel
open System.Windows
open System.Windows.Media
open System.Windows.Controls

[<AllowNullLiteral()>]
type BlockOfTimeViewModel(b : TimeMapEntry, classColor : Brush, className: string) =
    let day, time, summForClass = b
    let dt = TimeSeriesPhantom.TimeSeriesPeriodConversions.toDay day
    let startTime = TimeSeriesPhantom.TimeSeriesPeriodConversions.toTimeSpan time
    let classId, summ = summForClass
    let ts = TimeSpan.FromMinutes(float summ.minuteCount)

    member x.StartTime = dt.Add(startTime)
    member x.EndTime = dt.Add(startTime).AddMinutes(float summ.minuteCount)
    member x.Words = summ.wordCount
    member x.Hours = ts.Hours
    member x.Minutes = ts.Minutes
    member x.ClassName = className
    member x.ClassColor = classColor

type DesignTimeBlockOfTimeViewModel() =
    let now = DateTimeOffset.Now
    member x.StartTime = now.AddHours(-2.0)
    member x.EndTime = now.AddHours(-1.0)
    member x.Words = 2000
    member x.Hours = 0
    member x.Minutes = 34
    member x.ClassName = "Design Time"
    member x.ClassColor = (List.head Theme.customColors).back

type VisualLogData(provider : ITimeMapDataProvider, config : UserGlobalConfiguration) =
    member x.Provider = provider
    member x.Config = config

type DailyTimeSeriesBasedTimeMapProvider(samples : WithDate<MeasureForClass<ActivitySummary>> list, cfg : UserGlobalConfiguration) =   
    let limitToClasses =  Dated.unWrap >> (ClassificationForClassMeasurement.createLimitToClassesFilter (ClassificationClassFilterUtils.selectedClassesOrAll cfg.classification))
    let noiseReduction = FocusSessionCalculator.toSessions cfg.classification.idleMap
    let isLargeEnoughAndNotIdle (fs : FocusSession) = not fs.idle && (let ts = fs.endTime - fs.startTime in ts.TotalMinutes > 1.0)
    let removeSmall = List.filter isLargeEnoughAndNotIdle
    let filterAndReduce = (List.filter limitToClasses) >> List.sortBy (fun s -> (fst s)) >> noiseReduction >> removeSmall

    let activitySampleToBlock (fs : FocusSession) : MeasureForClass<ActivitySummary> =
        let summ = {ActivitySummary.wordCount = fs.words; ActivitySummary.minuteCount = let ts = fs.endTime.Subtract(fs.startTime) in (ts.TotalMinutes |> round |> int)}
        (fs.sessionClassId, summ)
        
    let activitySamplesToBlocks = List.map activitySampleToBlock

    let dayEntry e = 
        assert (List.length e = 1)
        let x = List.head e
        activitySampleToBlock x
        
    let emptyDay : DayMap = TimeSeriesPhantom.TimeSeries.mkEmpty |> TimeSeriesPhantom.TimeSeries.sort
        
    let dailyData (samples : FocusSession list) : DayMap =
        let ts = TimeSeriesPhantom.TimeSeries.mkSeries (fun (fs : FocusSession) -> TimeSeriesPhantom.TimeSeriesPeriods.mkTimeOfDay fs.startTime) dayEntry samples
        ts
            |> TimeSeriesPhantom.TimeSeries.sort

    let samplesAndClasses = filterAndReduce (List.rev samples)
    
    let timeMap = TimeSeriesPhantom.TimeSeries.mkSeries (fun (fs : FocusSession) -> TimeSeriesPhantom.TimeSeriesPeriods.mkDaily fs.startTime) dailyData samplesAndClasses
        

    interface ITimeMapDataProvider with
        member tm.BrushesForClass c = cfg.classification.colorMap c
        member tm.TimeMap = 
            assert (TimeSeriesPhantom.TimeSeries.length timeMap <= 7)
            let endDt = DataDateRangeFilterUtils.endDt cfg.dateRangeFilter
            let maxDt = TimeSeriesPhantom.TimeSeriesPeriods.mkDaily endDt
            let ts = timeMap
                            |> TimeSeriesPhantom.TimeSeries.regular (fun _ -> emptyDay) 7 maxDt
                            |> TimeSeriesPhantom.TimeSeries.reverse
            ts


type VisualLogWidgetViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<VisualLogData>(wds, true)
    
    let mutable data : VisualLogData = VisualLogData(EmptyTimeMapDataProvider(), EmptyConfiguration.empty)
    let mutable hoverDetails : BlockOfTimeViewModel = null

    
    let readData (wd : WorkingData) : Async<VisualLogData> = 
        async {
            let timeMapProvider' = DailyTimeSeriesBasedTimeMapProvider(wd.lastNDaySamplesForCurrentClassifier, wd.configuration)
            return VisualLogData(timeMapProvider', wd.configuration)
         }
    let showData d =
            data <- d
            vm.TriggerPropertyChanged "TimeMapDataProvider"
            vm.TriggerPropertyChanged "BlockDetails"
            vm.TriggerPropertyChanged "SubTitle"

    member x.TimeMapDataProvider = data.Provider
    member x.ShowDetail (d : TimeMapEntry) : Unit =
        let day, time, summForClass = d
        let dt = TimeSeriesPhantom.TimeSeriesPeriodConversions.toDay day
        let ts = TimeSeriesPhantom.TimeSeriesPeriodConversions.toTimeSpan time
        let classId, summ = summForClass
        let colors = data.Config.classification.colorMap classId
        let name = data.Config.classification.classNames classId
        hoverDetails <- BlockOfTimeViewModel(d, colors.back, name)
        vm.TriggerPropertyChanged "BlockDetails"

    member x.BlockDetails = hoverDetails
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Visual Log"
    override x.SubTitle = 
        let filterEndDt =
                               match data.Config.dateRangeFilter with
                               | DataDateRangeFilter.NoFilter -> System.DateTimeOffset.Now
                               | DataDateRangeFilter.FilterDataTo(_, endDt) -> endDt
        let startDt = filterEndDt.AddDays(-6.0)
        sprintf "%s - %s" (Humanize.dateAndDay filterEndDt) (Humanize.dateAndDay startDt)
    

type VisualLogWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<VisualLogWidgetViewModel, VisualLogData>(wds, "VisualLogWidgetElement.xaml")

    let mutable viewModel : VisualLogWidgetViewModel option = None

    override x.CreateViewModel wds = 
        let vm = VisualLogWidgetViewModel(wds)
        viewModel <- Some vm
        vm
    override x.InitialLoad content =
        let timeMap = content.FindName("OverviewTimeMap") :?> TimeMapControl
        timeMap.ShowDetail.Add(fun d ->
            if Option.isSome viewModel then
                let vm = Option.get viewModel
                vm.ShowDetail d)
    
    
module VisualLogWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast VisualLogWidgetElement(wds)