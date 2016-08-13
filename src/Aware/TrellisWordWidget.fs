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
type WordTrellisCellDetail(s : Statistics.FiveNumberSummary<float>, d, h) =
    member x.Words = int s.median
    member x.WordsLow = int s.lowerQuartile
    member x.WordsHigh = int s.upperQuartile
    member x.DayOfWeek = d
    member x.HourOfDay = h

type WordTrellisClassDetail(c : string, b: Brush, cellDetails : WordTrellisCellDetail) =
    member x.ClassName = c
    member x.ClassColor = b
    member x.CellDetails = cellDetails


type DesignTimeTrellisWordsViewModel() =
    let details = ObservableCollection<WordTrellisClassDetail>()
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
        details.Add(WordTrellisClassDetail("Design Time 1", c1, WordTrellisCellDetail(s1, System.DayOfWeek.Wednesday, 2)))
        details.Add(WordTrellisClassDetail("Design Time 2", c2, WordTrellisCellDetail(s2, System.DayOfWeek.Wednesday, 2)))
    member x.DayHourMatrixProvider = DesignTimeDayHourMatrixDataProvider()
    member x.ClassDetails = details

[<NoComparison()>]
[<NoEquality()>]
type TrellisWordsViewModelData = {provider: IDayHourMatrixProvider; clsFilter : ClassificationClassFilter; colorMap : ClassIdentifier -> AssignedBrushes; classNames : ClassIdentifier -> string; formattedDateRange : string}

type TrellisWordsViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<TrellisWordsViewModelData>(wds, true)

    let mutable data = {
                            provider = EmptyDayHourMatrixProvider()
                            clsFilter = ClassificationClassFilter.NoFilter
                            colorMap = (fun _ -> Theme.awareAssignedBrushes)
                            classNames = (fun _ -> "")
                            formattedDateRange = System.String.Empty
                       }
    let details = ObservableCollection<WordTrellisClassDetail>()

    let readData (wd : WorkingData) : Async<TrellisWordsViewModelData> = 
        async {               
            let _, max, ts = DayHourData.dailyDataTo wd
            
            let convert (at : MeasureByClass<ActivitySummaryStatistics>) : DayHourMatrixHourData =
                let minStatsByClass = at |> ByClass.fmap (fun x -> x.wordStatistics) 
                let minStats = ByClass.unWrap minStatsByClass
                let wc = minStats.median
                assert (wc <= float max.wordCount)
                let x = (wc / float max.wordCount)

                {value =x; data = minStatsByClass ; brush = Theme.brushByClass wd.configuration.classification.colorMap at}

            
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
            |> ClassificationClassFilterUtils.map (WordTrellisClassDetail("Total", Theme.awareBrush, null)) (fun c -> WordTrellisClassDetail(c.className, (d.colorMap (ClassIdentifier c.id)).back, null))
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
                (fun (m : Statistics.FiveNumberSummary<float>) -> WordTrellisClassDetail("Total", Theme.awareBrush, WordTrellisCellDetail(m, day, hour)))
                (fun id (m : Statistics.FiveNumberSummary<float>) -> WordTrellisClassDetail(data.classNames id, (data.colorMap id).back, WordTrellisCellDetail(m, day, hour)))
        xs
            |> Seq.cast
            |> Seq.toList
            |> List.map (fun (m : MeasureByClass<Statistics.FiveNumberSummary<float>>) -> toVm m)
            |> List.iter details.Add
        vm.TriggerPropertyChanged "ClassDetails"

    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Trellis - Words"
    override x.SubTitle = data.formattedDateRange

type TrellisWordsWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<TrellisWordsViewModel, TrellisWordsViewModelData>(wds, "TrellisWordsWidgetElement.xaml")

    let mutable vm : TrellisWordsViewModel option = None

    override x.CreateViewModel wds = 
        let v = TrellisWordsViewModel(wds)
        vm <- Some v
        v

    override x.InitialLoad uc =
        let charts = uc.FindName("DayHourMatrixControl") :?> DayHourMatrixControl
        charts.ShowDetail.Add (fun (d, h, p) ->
                                        let v = Option.get vm
                                        v.ShowDetail d h p)
        

module TrellisWordsWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast TrellisWordsWidgetElement(wds)
