namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Controls.Composite    
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes

open System
open System.Windows
open System.Windows.Controls



type ProductivityByHourOfDayViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<ProductivityDistributionProviders>(wds, false)
    let emptyProvider = EmptyBoxPlotDataProvider()
    let mutable providers = ProductivityDistributionProviders(emptyProvider,emptyProvider,emptyProvider,[("Total", upcast Theme.awareBrush)])
    let readData (wd : WorkingData) = 
        async {
            let config = wd.configuration
                
            let cf = wd.configuration.classification.filter
            let zeroDay = ClassificationByClassMeasurement.zeros cf ActivitySummaryStatistics.Zero
            let summarize (xs : WithDate<MeasureByClass<ActivitySummary>> list) : MeasureByClass<ActivitySummaryStatistics> list =
                ClassificationByClassMeasurement.filterMap cf Summaries.summarize (xs |> List.map Dated.unWrap)

            let ts = 
                wd.storedSummaries.Value.hourly
                    |> TimeSeriesPhantom.TimeSeries.mkSeries  (Dated.dt >> TimeSeriesPhantom.TimeSeriesPeriods.mkHourOfDay) summarize
                    |> TimeSeriesPhantom.TimeSeries.complete (fun _ -> zeroDay)

            return TimeSeriesProductivityDistributionCharts.providers wd.configuration ts

         }
    let showData ps =
            providers <- ps
            vm.TriggerPropertyChanged "ProductivtyDistributionControlProviders"
    
    member x.ProductivtyDistributionControlProviders = providers
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Hour Of Day"
    

type ProductivityByHourOfDayWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<ProductivityByHourOfDayViewModel, ProductivityDistributionProviders>(wds, "ProductivityByHourOfDayWidgetElement.xaml")
    override x.CreateViewModel wds = ProductivityByHourOfDayViewModel(wds)
    
module ProductivityByHourOfDayWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast ProductivityByHourOfDayWidgetElement(wds)
