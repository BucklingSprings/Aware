namespace BucklingSprings.Aware.Controls.Charts

open System
open System.Windows.Media
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Common.Themes

[<RequireQualifiedAccessAttribute()>]
[<NoComparison()>]
type ChartPoints<'p> = {values : ('p * obj) list; isHighLighted : bool}

[<RequireQualifiedAccessAttribute()>]
type ScalarChartPoints = ChartPoints<float>

[<RequireQualifiedAccessAttribute()>]
type BoxPlotPoints = ChartPoints<FiveNumberSummary<float>>

[<RequireQualifiedAccessAttribute()>]
type ChartAxisPoint =  {value : float; humanized : string}

[<RequireQualifiedAccessAttribute()>]
type ScalarSeriesDisplayType =
    | Bars
    | TrendLine


[<RequireQualifiedAccessAttribute()>]
[<NoComparison()>]
type ScalarSeriesChart = {points : ScalarChartPoints list; xAxis : ChartAxisPoint list; yAxis : ChartAxisPoint list; displayType : ScalarSeriesDisplayType}

[<RequireQualifiedAccess()>]
[<NoComparison()>]
type BoxPlot = {points : BoxPlotPoints list; xAxis : ChartAxisPoint list; yAxis : ChartAxisPoint list; plotRangeOnly : bool}

[<RequireQualifiedAccess()>]
[<NoComparison()>]
type CircleChart = {points : (float * obj) list}

type IScalarSeriesDataProvider =
    abstract ChartData : ScalarSeriesChart
    abstract BrushForDataPoint : float -> obj -> Brush

type IBoxPlotDataProvider =
    abstract ChartData : BoxPlot
    abstract BrushForDataPoint : FiveNumberSummary<float> -> obj -> Brush

type ICircleChartDataProvider =
    abstract ChartData : CircleChart
    abstract BrushForDataPoint : float -> obj -> Brush

type EmptyScalarSeriesDataProvider() =
    interface IScalarSeriesDataProvider with
        member x.ChartData =
            let pnts = [for i in 0 .. 6 -> {ScalarChartPoints.values = []; ScalarChartPoints.isHighLighted = false} ] 
            let xAx = [for i in 0 .. 3 -> {ChartAxisPoint.humanized = (Humanize.dateTimeOffset (let now = System.DateTimeOffset.Now in now.AddDays(-1.0 * float i))); ChartAxisPoint.value = (float i / 4.0)} ]
            let yAx = [for i in 1 .. 2 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = (float i / 2.0)} ]
            {ScalarSeriesChart.points = pnts; ScalarSeriesChart.xAxis = xAx; ScalarSeriesChart.yAxis = yAx; ScalarSeriesChart.displayType = ScalarSeriesDisplayType.Bars}
        member x.BrushForDataPoint _ _ = upcast Brushes.Black

type DesignTimerTrendDataProvider() =
    let rand = Random()
    interface IScalarSeriesDataProvider with
        member x.ChartData =
            let pnts = [for i in 0 .. 6 -> {ScalarChartPoints.values = [float (rand.Next(0,6))  / 7.0, () :> obj]; ScalarChartPoints.isHighLighted = false} ] 
            let xAx = [for i in 0 .. 3 -> {ChartAxisPoint.humanized = (Humanize.dateTimeOffset (let now = System.DateTimeOffset.Now in now.AddDays(-1.0 * float i))); ChartAxisPoint.value = (float i / 4.0)} ]
            let yAx = [for i in 1 .. 2 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = (float i / 2.0)} ]
            {ScalarSeriesChart.points = pnts; ScalarSeriesChart.xAxis = xAx; ScalarSeriesChart.yAxis = yAx; ScalarSeriesChart.displayType = ScalarSeriesDisplayType.TrendLine}
        member x.BrushForDataPoint _ _ = upcast Theme.awareBrush

type EmptyCircleChartDataProvider() =
    interface ICircleChartDataProvider with
        member x.ChartData = {CircleChart.points = []}
        member x.BrushForDataPoint _ _ = upcast Brushes.Black

type EmptyBoxPlotDataProvider() =
    interface IBoxPlotDataProvider with
        member x.ChartData =
            let fiveNum : FiveNumberSummary<float> = {
                            FiveNumberSummary.minimum = 0.
                            FiveNumberSummary.maximum = 0.
                            FiveNumberSummary.lowerQuartile = 0.
                            FiveNumberSummary.upperQuartile = 0.
                            FiveNumberSummary.median= 0.
                          }
            let pnts = [{ScalarChartPoints.values = [(fiveNum, obj())]; ScalarChartPoints.isHighLighted = false}] 
            let xAx = [for i in 1 .. 7 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = float i / 7.0} ]
            let yAx = [for i in 0 .. 4 .. 24 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = float i / 24.0} ]
            {BoxPlot.points = pnts; BoxPlot.xAxis = xAx; BoxPlot.yAxis = yAx; BoxPlot.plotRangeOnly = false}
        member x.BrushForDataPoint _ _ = upcast Theme.awareBrush

type DesigntimeBoxPlotDataProvider() =
    interface IBoxPlotDataProvider with
        member x.ChartData =
            let r = System.Random()
            let pnts = [for i in 1 .. 7 -> {ScalarChartPoints.values = [(StatisticalSummary.randomForDesignTimeNormalized(), obj()); (StatisticalSummary.randomForDesignTimeNormalized(), obj())]; ScalarChartPoints.isHighLighted = false}] 
            let xAx = [for i in 1 .. 7 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = (float i  - 0.5)/ 7.0} ]
            let yAx = [{ChartAxisPoint.humanized = "1"; ChartAxisPoint.value = 0.5}; {ChartAxisPoint.humanized = "2"; ChartAxisPoint.value = 1.0} ]
            {BoxPlot.points = pnts; BoxPlot.xAxis = xAx; BoxPlot.yAxis = yAx; BoxPlot.plotRangeOnly = false}
        member x.BrushForDataPoint _ _ = upcast Theme.awareBrush

type DesigntimeOnlyRangeBoxPlotDataProvider() =
    interface IBoxPlotDataProvider with
        member x.ChartData =
            let r = System.Random()
            let pnts = [for i in 1 .. 100 -> {ScalarChartPoints.values = [(StatisticalSummary.randomForDesignTimeNormalized(), obj()); (StatisticalSummary.randomForDesignTimeNormalized(), obj())]; ScalarChartPoints.isHighLighted = false}] 
            let xAx = [for i in 1 .. 7 -> {ChartAxisPoint.humanized = string i; ChartAxisPoint.value = (float i  - 0.5)/ 7.0} ]
            let yAx = [{ChartAxisPoint.humanized = "1"; ChartAxisPoint.value = 0.5}; {ChartAxisPoint.humanized = "2"; ChartAxisPoint.value = 1.0} ]
            {BoxPlot.points = pnts; BoxPlot.xAxis = xAx; BoxPlot.yAxis = yAx; BoxPlot.plotRangeOnly = true}
        member x.BrushForDataPoint _ _ = upcast Theme.awareBrush