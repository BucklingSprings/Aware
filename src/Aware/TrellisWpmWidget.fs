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

open System.Windows.Media
open System.Collections.ObjectModel



open System.Windows

[<AllowNullLiteral()>]
type WordsPerMinuteTrellisCellDetail(s : Statistics.FiveNumberSummary<float>, d, h) =
    member x.WordsPerMinute = int s.median
    member x.WordsPerMinuteLow = int s.lowerQuartile
    member x.WordsPerMinuteHigh = int s.upperQuartile
    member x.DayOfWeek = d
    member x.HourOfDay = h

type WordsPerMinuteTrellisClassDetail(c : string, b: Brush, cellDetails : WordsPerMinuteTrellisCellDetail) =
    member x.ClassName = c
    member x.ClassColor = b
    member x.CellDetails = cellDetails


type DesignTimeTrellisWordsPerMinuteViewModel() =
    let details = ObservableCollection<WordsPerMinuteTrellisClassDetail>()
    do
        let c1 = (Theme.customColors |> List.head).back
        let c2 = (Theme.customColors |> List.tail |> List.head).back
        let s1 =   {
                        FiveNumberSummary.maximum = 100.0
                        FiveNumberSummary.upperQuartile = 50.0
                        FiveNumberSummary.median = 25.0
                        FiveNumberSummary.lowerQuartile = 20.0
                        FiveNumberSummary.minimum = 0.0
                    }
        let s2 =   {
                        FiveNumberSummary.maximum = 100.0
                        FiveNumberSummary.upperQuartile = 50.0
                        FiveNumberSummary.median = 22.0
                        FiveNumberSummary.lowerQuartile = 20.0
                        FiveNumberSummary.minimum = 0.0
                    }
        details.Add(WordsPerMinuteTrellisClassDetail("Design Time 1", c1, WordsPerMinuteTrellisCellDetail(s1, System.DayOfWeek.Wednesday, 2)))
        details.Add(WordsPerMinuteTrellisClassDetail("Design Time 2", c2, WordsPerMinuteTrellisCellDetail(s2, System.DayOfWeek.Wednesday, 2)))
    member x.DayHourMatrixProvider = DesignTimeDayHourMatrixDataProvider()
    member x.ClassDetails = details

[<NoComparison()>]
[<NoEquality()>]
type TrellisWordsPerMinutesViewModelData = {provider: IDayHourMatrixProvider; clsFilter : ClassificationClassFilter; colorMap : ClassIdentifier -> AssignedBrushes; classNames : ClassIdentifier -> string; formattedDateRange : string}

type TrellisWordsPerMinutesViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<TrellisWordsPerMinutesViewModelData>(wds, true)

    let mutable data = {
                            provider = EmptyDayHourMatrixProvider()
                            clsFilter = ClassificationClassFilter.NoFilter
                            colorMap = (fun _ -> Theme.awareAssignedBrushes)
                            classNames = (fun _ -> "")
                            formattedDateRange = System.String.Empty
                       }
    let details = ObservableCollection<WordsPerMinuteTrellisClassDetail>()

    let readData (wd : WorkingData) : Async<TrellisWordsPerMinutesViewModelData> = 
        async {               
            
            
            let maxWpm, _, ts = DayHourData.dailyDataTo wd
            
            let convert (at : MeasureByClass<ActivitySummaryStatistics>) : DayHourMatrixHourData =
                let wpmStatsByClass = at |> ByClass.fmap (fun x -> x.wordPerMinuteStatistics) 
                let wpmStats = ByClass.unWrap wpmStatsByClass
                let wc = wpmStats.median
                assert (wc <= float maxWpm)
                let x = (wc / float maxWpm)

                {value =x; data = wpmStatsByClass ; brush = Theme.brushByClass wd.configuration.classification.colorMap at}

            
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
            |> ClassificationClassFilterUtils.map (WordsPerMinuteTrellisClassDetail("Total", Theme.awareBrush, null)) (fun c -> WordsPerMinuteTrellisClassDetail(c.className, (d.colorMap (ClassIdentifier c.id)).back, null))
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
                (fun (m : Statistics.FiveNumberSummary<float>) -> WordsPerMinuteTrellisClassDetail("Total", Theme.awareBrush, WordsPerMinuteTrellisCellDetail(m, day, hour)))
                (fun id (m : Statistics.FiveNumberSummary<float>) -> WordsPerMinuteTrellisClassDetail(data.classNames id, (data.colorMap id).back, WordsPerMinuteTrellisCellDetail(m, day, hour)))
        xs
            |> Seq.cast
            |> Seq.toList
            |> List.map (fun (m : MeasureByClass<Statistics.FiveNumberSummary<float>>) -> toVm m)
            |> List.iter details.Add
        vm.TriggerPropertyChanged "ClassDetails"

    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Trellis - Words Per Minute"
    override x.SubTitle = data.formattedDateRange

type TrellisWordsPerMinutesWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<TrellisWordsPerMinutesViewModel, TrellisWordsPerMinutesViewModelData>(wds, "TrellisWordsPerMinutesWidgetElement.xaml")

    let mutable vm : TrellisWordsPerMinutesViewModel option = None

    override x.CreateViewModel wds = 
        let v = TrellisWordsPerMinutesViewModel(wds)
        vm <- Some v
        v

    override x.InitialLoad uc =
        let charts = uc.FindName("DayHourMatrixControl") :?> DayHourMatrixControl
        charts.ShowDetail.Add (fun (d, h, p) ->
                                        let v = Option.get vm
                                        v.ShowDetail d h p)
        

module TrellisWordsPerMinutesWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast TrellisWordsPerMinutesWidgetElement(wds)
