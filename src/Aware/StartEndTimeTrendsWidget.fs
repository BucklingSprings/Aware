namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Common.UserConfiguration

open System
open System.Windows

type DesignTimeStartEndTimeTrendsViewModel() =
    member x.DayLengthChartProvider = DesigntimeOnlyRangeBoxPlotDataProvider()
    member x.StartTime = 322
    member x.EndTime = 1213

type StartEndTimeTrendsViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<IBoxPlotDataProvider * string>(wds, false)
    let mutable providers = EmptyBoxPlotDataProvider() :> IBoxPlotDataProvider
    let mutable startTime = -1
    let mutable endTime = -1
    let mutable subTitle = String.Empty
    let readData (wd : WorkingData) = 
        async {
            let dayLengths = wd.storedSummaries.Value.dayLengths

            let dayLengthToFivenumberSummary (xs : WithDate<DayLength> list) : (FiveNumberSummary<float> * DateTimeOffset) =
                assert (List.length xs = 1)
                let x = List.head xs
                let dayLength = Dated.unWrap x
                let fn = {
                                FiveNumberSummary.minimum =  0.0
                                FiveNumberSummary.lowerQuartile =  StartAndEndTime.toMinutesFromStartOfDay dayLength.startTime |> float
                                FiveNumberSummary.median =  0.0
                                FiveNumberSummary.upperQuartile =  StartAndEndTime.toMinutesFromStartOfDay dayLength.endTime |> float
                                FiveNumberSummary.maximum =  0.0
                            }
                (fn, Dated.dt x)
                

            
            let dayCount = let ts = wd.maxmimumDate.Subtract(wd.minimumDate) in ts.TotalDays |> ceil |> int
            
            let ts = 
                TimeSeries.mkSeries (Dated.dt >> TimeSeriesPeriods.mkDaily) dayLengthToFivenumberSummary dayLengths
                    |> TimeSeries.regular (fun p -> (StatisticalSummary.zerof, TimeSeriesPeriodConversions.toDay p)) dayCount (TimeSeriesPeriods.mkDaily wd.maxmimumDate)
                
            let periodAndPoints _ p (fn,dt) = (TimeSeriesPeriods.humanize p, [(StatisticalSummary.normalize fn (24.0 * 60.0), (fn, dt) :> obj)])

            let periods,points = (ts |> TimeSeries.extractDataAndPeriodi periodAndPoints) |> List.unzip

            let prov = ChartProviderHelper.rangeOnlyBoxPlot (Humanize.hoursFromStartOfDayToTimeFormat 12) (Humanize.hoursFromStartOfDayToTimeFormat 24) periods points (fun _ -> upcast Theme.awareBrush)

            return (prov, DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter wd.minimumDate wd.maxmimumDate)

         }
    let showData d =
        providers <- fst d
        subTitle <- snd d
        startTime <- -1
        endTime <- -1
        vm.TriggerPropertyChanged "DayLengthChartProvider"
        vm.TriggerPropertyChanged "StartTime"
        vm.TriggerPropertyChanged "EndTime"
        vm.TriggerPropertyChanged "SubTitle"

    let showTimes s e dt =
        startTime <- if s = 0 then -1 else s
        endTime <- if e = 0 then -1 else e
        subTitle <- Humanize.dateAndDay dt
        vm.TriggerPropertyChanged "StartTime"
        vm.TriggerPropertyChanged "EndTime"
        vm.TriggerPropertyChanged "SubTitle"

    
    member x.DayLengthChartProvider = providers
    member x.ShowDetail (boxPlotPoints : obj list) =
        assert (List.length boxPlotPoints = 1)
        let fn = List.head boxPlotPoints
        match fn with
            | :? (FiveNumberSummary<float> * DateTimeOffset) as p -> 
                    let fn, dt = p
                    showTimes (int fn.lowerQuartile) (int fn.upperQuartile) dt
            | _ -> ()
        ()
    member x.StartTime = startTime
    member x.EndTime = endTime
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Start & End Time Trends"
    override x.SubTitle = subTitle
    

type StartEndTimeTrendsWidgetElement(wds : WorkingDataService) as we =
    inherit StandardWidgetElementBase<StartEndTimeTrendsViewModel, IBoxPlotDataProvider * string>(wds, "StartEndTimeTrendsWidgetElement.xaml")
    override x.InitialLoad content =
        let startEndTimeChart = content.FindName("StartEndTimeBoxPlotControl") :?> BoxPlotControl
        startEndTimeChart.PointHoverEnter.Add(fun e -> if Option.isSome we.ViewModel then Option.get(we.ViewModel).ShowDetail(e.BoxPlotPoints.values |> List.map snd))
        
    override x.CreateViewModel wds = StartEndTimeTrendsViewModel(wds)

module StartEndTimeTrendsWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast StartEndTimeTrendsWidgetElement(wds)

