namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Controls.Composite

open System
open System.ComponentModel
open System.Threading
open System.Windows
open System.Windows.Controls

type NoOpScatterPlotWidgetViewModel(wds : WorkingDataService) =
    inherit WidgetViewModelBase<Unit>(wds, false)
    
    let readData (wd : WorkingData) = 
        async {
            return ()
         }
    let showData prod = ()

    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Placeholder"
    

type NoOpScatterPlotWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<NoOpScatterPlotWidgetViewModel, Unit>(wds, "NoOpScatterPlotWidgetElement.xaml")
    override x.CreateViewModel wds = NoOpScatterPlotWidgetViewModel(wds)        
   
module NoOpScatterPlotWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast NoOpScatterPlotWidgetElement(wds)