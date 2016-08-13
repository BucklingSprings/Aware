namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Core.Diagnostics

open System.Windows.Media

type ScalarChartProvider(chart, colorMap) =
    interface IScalarSeriesDataProvider with
        member x.ChartData = chart
        member x.BrushForDataPoint _ d = colorMap d

type BoxPlotChartProvider(chart, colorMap) =
    interface IBoxPlotDataProvider with
        member x.ChartData = chart
        member x.BrushForDataPoint _ d = colorMap d


module ChartProviderHelper =
    
    let twoPointYAxis mid max =
        [
            {ChartAxisPoint.humanized = mid; ChartAxisPoint.value = 0.5} 
            {ChartAxisPoint.humanized = max; ChartAxisPoint.value = 1.0} 
        ]

    let uniformAxis (labels : string list) = 
        let count = List.length labels
        
        let axisPoint (index, label) = {ChartAxisPoint.humanized = label; ChartAxisPoint.value = (float index) / (float count)}
        let n = int (ceil ((float count) / (3.0)))
        let selectedLabels = labels |> List.mapi (fun i s -> (i, s)) |> List.choose (fun (i, l) -> if i % n = 0 then Some (i, l) else None)
        selectedLabels |> List.map axisPoint

    let uniformAxisAllLabels (labels : string list) = 
        let count = List.length labels
        let axisPoint index label = {ChartAxisPoint.humanized = label; ChartAxisPoint.value = (float index) / (float count)}
        labels |> List.mapi axisPoint

    let uniformAxisLabelsEvery (n : int) (labels : string list) = 
        let count = List.length labels
        let axisPoint (index, label) = {ChartAxisPoint.humanized = label; ChartAxisPoint.value = (float index) / (float count)}
        let selectedLabels = labels |> List.mapi (fun i s -> (i, s)) |> List.choose (fun (i, l) -> if i % n = 0 then Some (i, l) else None)
        selectedLabels |> List.map axisPoint

    let scalarChart midY maxY xLabels pointValues displayType colorMap =
        let points = pointValues |> List.map (fun p -> {ChartPoints.values = p; ScalarChartPoints.isHighLighted = false})
        let chart = {ScalarSeriesChart.points = points; ScalarSeriesChart.xAxis = uniformAxis xLabels; ScalarSeriesChart.yAxis = twoPointYAxis midY maxY; ScalarSeriesChart.displayType = displayType}
        ScalarChartProvider(chart, colorMap) :> IScalarSeriesDataProvider

    let rangeOnlyBoxPlot midY maxY xLabels pointValues colorMap =
        let points = pointValues |> List.map (fun p -> {ChartPoints.values = p; ScalarChartPoints.isHighLighted = false})
        let chart = {BoxPlot.points = points; BoxPlot.xAxis = uniformAxis xLabels; BoxPlot.yAxis = twoPointYAxis midY maxY; BoxPlot.plotRangeOnly = true}
        BoxPlotChartProvider(chart, colorMap) :> IBoxPlotDataProvider

    let normalize x y = if y = 0 then 0.0 else (float x) / (float y)

type FiveNumberSummaryBoxPlotDataProvider(points, colorMap, midY, maxY, xLabels) =
    interface IBoxPlotDataProvider with
        member x.ChartData =
            let point x = {ScalarChartPoints.values = x; ScalarChartPoints.isHighLighted = false}
            let xAx = if (List.length xLabels) <= 7 then ChartProviderHelper.uniformAxisAllLabels xLabels else ChartProviderHelper.uniformAxisLabelsEvery 3 xLabels
            let yAx = ChartProviderHelper.twoPointYAxis midY maxY
            {BoxPlot.points = points |> List.map point; BoxPlot.xAxis = xAx; BoxPlot.yAxis = yAx; BoxPlot.plotRangeOnly = false}
        member x.BrushForDataPoint _ o = colorMap o

type CircleChartPercentageDataPoint<'a>(rawValue : int, percentage : float, data : 'a, brush : Brush) =
    member val RawValue = rawValue with get
    member val Percentage = percentage with get
    member val Data = data with get
    member val Brush = brush with get

type CirclePlotPercentageDataProvider<'a>(values : (int * 'a) list, colorMap : 'a -> Brush) =
    let total = List.sumBy fst values
    let toPercentage x = if total = 0 then 0.0 else (float x) / (float total)
    let toPoint (x,o) = 
        let p = toPercentage x
        let brush = colorMap o
        (p, CircleChartPercentageDataPoint(x, p, o, brush) :> obj)
    let points = values |> List.map toPoint |> List.sortBy (fun (p,_) -> -p)
    interface ICircleChartDataProvider with    
        member x.ChartData =
            {CircleChart.points = points}
        member x.BrushForDataPoint _ o = 
            match o with
            | :? CircleChartPercentageDataPoint<'a> as c -> colorMap c.Data
            | _ -> Theme.otherColors.back