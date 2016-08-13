namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Common.UserConfiguration

open System.Windows

type DesignTimeStartEndTimeDistributionViewModel() =
    member x.StartTime = 345
    member x.StartTimeLow = 245
    member x.StartTimeHigh = 600

    member x.EndTime = 924
    member x.EndTimeLow = 623
    member x.EndTimeHigh = 1000

type StartEndTimeDistributionViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<FiveNumberSummary<int> * FiveNumberSummary<int> * string>(wds, false)

    let mutable startTimeDist = StatisticalSummary.someNegativeValue
    let mutable endTimeDist = StatisticalSummary.someNegativeValue
    let mutable subTitle = System.String.Empty
    
    let readData (wd : WorkingData) = 
        async {
            let dayLengths = wd.storedSummaries.Value.dayLengths
            let s (x : DayLength) = x.startTime
            let e (x : DayLength) = x.endTime
            let startTimes = dayLengths |> List.map (Dated.unWrap >> s >> StartAndEndTime.toMinutesFromStartOfDay)
            let endTimes = dayLengths |> List.map (Dated.unWrap >> e >> StartAndEndTime.toMinutesFromStartOfDay)

            let startDist = startTimes |> StatisticalSummary.summarize |> StatisticalSummary.round
            let endDist = endTimes |> StatisticalSummary.summarize |> StatisticalSummary.round

            return (startDist, endDist, DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter wd.minimumDate wd.maxmimumDate)
         }
    let showData d =
        let s,e,t = d
        startTimeDist <- s
        endTimeDist <- e
        subTitle <- t
        vm.TriggerPropertyChanged("StartTime")
        vm.TriggerPropertyChanged("StartTimeLow")
        vm.TriggerPropertyChanged("StartTimeHigh")
        vm.TriggerPropertyChanged("EndTime")
        vm.TriggerPropertyChanged("EndTimeLow")
        vm.TriggerPropertyChanged("EndTimeHigh")
        vm.TriggerPropertyChanged("SubTitle")

    
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Typical Start & End Times"
    override x.SubTitle = subTitle
    member x.StartTime = startTimeDist.median
    member x.StartTimeLow = startTimeDist.lowerQuartile
    member x.StartTimeHigh = startTimeDist.upperQuartile
    member x.EndTime = endTimeDist.median
    member x.EndTimeLow = endTimeDist.lowerQuartile
    member x.EndTimeHigh = endTimeDist.upperQuartile

type StartEndTimeDistributionWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<StartEndTimeDistributionViewModel, FiveNumberSummary<int> * FiveNumberSummary<int> * string>(wds, "StartEndTimeDistributionWidgetElement.xaml")
    override x.CreateViewModel wds = StartEndTimeDistributionViewModel(wds)

module StartEndTimeDistributionWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast StartEndTimeDistributionWidgetElement(wds)

