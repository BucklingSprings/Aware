namespace BucklingSprings.Aware.Controls.Charts

open System.Windows.Media


[<RequireQualifiedAccessAttribute()>]
[<NoComparison()>]
///<summary>
///x and y co-ordinates should be passed as logical values relative to x-max and y-max rounded value respectively
///</summary>
type ScatterPlotPoint = {index : int; x : float; y : float; data : obj}

[<RequireQualifiedAccessAttribute()>]
///<summary>
///value should be passed as logical value relative to x-max and y-max rounded value respectively
///</summary>
type ScatterPlotAxisPoint =  ChartAxisPoint

[<RequireQualifiedAccessAttribute()>]
[<NoComparison()>]
type ScatterPlotChart = {points : ScatterPlotPoint list; xAxis : ScatterPlotAxisPoint list; yAxis: ScatterPlotAxisPoint list}

type IScatterPlotDataProvider =
    abstract ChartData : ScatterPlotChart
    abstract BrushForSeriesByIndex : int -> Brush
   
type EmptyScatterPlotDataProvider() =
    interface IScatterPlotDataProvider with
        member x.ChartData =
            let pnts = List.empty<ScatterPlotPoint>
            //let pnts = [for i in 1 .. 7 -> {ScatterPlotPoint.index = 1; ScatterPlotPoint.x = float i/float 8.0; ScatterPlotPoint.y = float i/float 8.0; ScatterPlotPoint.data = string i} ]
            let ax = [for i in 1 .. 7 -> {ScatterPlotAxisPoint.humanized = string i; ScatterPlotAxisPoint.value = float (float i/float 8)} ] 
            {points = pnts; xAxis = ax; yAxis=ax}
        member x.BrushForSeriesByIndex i = upcast (if i < 1 then Brushes.Black else Brushes.Red)
        