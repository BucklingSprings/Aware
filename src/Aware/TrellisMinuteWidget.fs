namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Common.UserConfiguration

open System.Windows
open System.Windows.Media
open System.Collections.ObjectModel





[<AllowNullLiteral()>]
type MinuteTrellisCellDetail(s : Statistics.FiveNumberSummary<float>, d, h) =
    member x.Minutes = int s.median
    member x.MinutesLow = int s.lowerQuartile
    member x.MinutesHigh = int s.upperQuartile
    member x.DayOfWeek = d
    member x.HourOfDay = h

type MinuteTrellisClassDetail(c : string, b: Brush, cellDetails : MinuteTrellisCellDetail) =
    member x.ClassName = c
    member x.ClassColor = b
    member x.CellDetails = cellDetails


type DesignTimeTrellisMinutesViewModel() =
    let details = ObservableCollection<MinuteTrellisClassDetail>()
    do
        let c1 = (Theme.customColors |> List.head).back
        let c2 = (Theme.customColors |> List.tail |> List.head).back
        let s1 =   {
                        FiveNumberSummary.maximum = 60.0
                        FiveNumberSummary.upperQuartile = 55.0
                        FiveNumberSummary.median = 25.0
                        FiveNumberSummary.lowerQuartile = 20.0
                        FiveNumberSummary.minimum = 0.0
                    }
        let s2 =   {
                        FiveNumberSummary.maximum = 60.0
                        FiveNumberSummary.upperQuartile = 28.0
                        FiveNumberSummary.median = 22.0
                        FiveNumberSummary.lowerQuartile = 20.0
                        FiveNumberSummary.minimum = 0.0
                    }
        details.Add(MinuteTrellisClassDetail("Design Time 1", c1, MinuteTrellisCellDetail(s1, System.DayOfWeek.Wednesday, 2)))
        details.Add(MinuteTrellisClassDetail("Design Time 2", c2, MinuteTrellisCellDetail(s2, System.DayOfWeek.Wednesday, 2)))
    member x.DayHourMatrixProvider = DesignTimeDayHourMatrixDataProvider()
    member x.ClassDetails = details

[<NoComparison()>]
[<NoEquality()>]
type TrellisMinutesViewModelData = {provider: IDayHourMatrixProvider; clsFilter : ClassificationClassFilter; colorMap : ClassIdentifier -> AssignedBrushes; classNames : ClassIdentifier -> string; formattedDateRange : string}

type TrellisMinutesViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<TrellisMinutesViewModelData>(wds, true)

    let mutable data = {
                            provider = EmptyDayHourMatrixProvider()
                            clsFilter = ClassificationClassFilter.NoFilter
                            colorMap = (fun _ -> Theme.awareAssignedBrushes)
                            classNames = (fun _ -> "")
                            formattedDateRange = System.String.Empty
                       }
    let details = ObservableCollection<MinuteTrellisClassDetail>()

    let readData (wd : WorkingData) : Async<TrellisMinutesViewModelData> = 
        async {               
            let _, max, ts = DayHourData.dailyDataTo wd
            
            let convert (at : MeasureByClass<ActivitySummaryStatistics>) : DayHourMatrixHourData =
                let minuteStatsByClass = at |> ByClass.fmap (fun x -> x.minuteStatistics) 
                let minuteStats = ByClass.unWrap minuteStatsByClass
                let wc = minuteStats.median
                assert (wc <= float 60.0)
                let x = (wc / float 60.0)

                {value =x; data = minuteStatsByClass ; brush = Theme.brushByClass wd.configuration.classification.colorMap at}

            
            let convert' = TimeSeriesPhantom.TimeSeries.fmap (List.map convert) |> TimeSeriesPhantom.TimeSeries.fmap
            let ts' = convert' ts
            let p = {
                        new IDayHourMatrixProvider with
                            member x.DayHourMatrix = ts'
                    }
            return {
                provider = p; 
                clsFilter = wd.configuration.classification.filter; 
                colorMap = wd.configuration.classification.colorMap; 
                classNames = wd.configuration.classification.classNames
                formattedDateRange = DataDateRangeFilterUtils.formatted wd.configuration.dateRangeFilter  wd.minimumDate wd.maxmimumDate
                }
        }
    let showData d =
        data <- d
        details.Clear()
        
        d.clsFilter
            |> ClassificationClassFilterUtils.map (MinuteTrellisClassDetail("Total", Theme.awareBrush, null)) (fun c -> MinuteTrellisClassDetail(c.className, (d.colorMap (ClassIdentifier c.id)).back, null))
            |> List.iter details.Add
                    
        vm.TriggerPropertyChanged "SubTitle"            
        vm.TriggerPropertyChanged "ClassDetails"
        vm.TriggerPropertyChanged "DayHourMatrixProvider"

    member x.DayHourMatrixProvider = data.provider
    member x.ClassDetails = details
    member x.ShowDetail d h xs = 
        let day = TimeSeriesPhantom.TimeSeriesPeriods.dayOfWeek d
        let hour = TimeSeriesPhantom.TimeSeriesPeriods.hourOfDay h
        details.Clear()
        let toVm = 
            ByClass.unWrap' 
                (fun (m : Statistics.FiveNumberSummary<float>) -> MinuteTrellisClassDetail("Total", Theme.awareBrush, MinuteTrellisCellDetail(m, day, hour)))
                (fun id (m : Statistics.FiveNumberSummary<float>) -> MinuteTrellisClassDetail(data.classNames id, (data.colorMap id).back, MinuteTrellisCellDetail(m, day, hour)))
        xs
            |> Seq.cast
            |> Seq.toList
            |> List.map (fun (m : MeasureByClass<Statistics.FiveNumberSummary<float>>) -> toVm m)
            |> List.iter details.Add
        vm.TriggerPropertyChanged "ClassDetails"

    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Trellis - Minutes"
    override x.SubTitle = data.formattedDateRange

type TrellisMinutesWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<TrellisMinutesViewModel, TrellisMinutesViewModelData>(wds, "TrellisMinutesWidgetElement.xaml")

    let mutable vm : TrellisMinutesViewModel option = None

    override x.CreateViewModel wds = 
        let v = TrellisMinutesViewModel(wds)
        vm <- Some v
        v

    override x.InitialLoad uc =
        let charts = uc.FindName("DayHourMatrixControl") :?> DayHourMatrixControl
        charts.ShowDetail.Add (fun (d, h, p) ->
                                        let v = Option.get vm
                                        v.ShowDetail d h p)
        

module TrellisMinutesWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast TrellisMinutesWidgetElement(wds)
