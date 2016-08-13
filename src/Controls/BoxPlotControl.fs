namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Media.Effects
open BucklingSprings.Aware.Common.Themes

open BucklingSprings.Aware.Core.Statistics

open System.Windows
open System.Windows.Media

type BoxPlotRegionHoverEventArgs(index : int, points : BoxPlotPoints, userAction : bool) =
    inherit System.EventArgs()
    member val PointIndex = index with get
    member val BoxPlotPoints = points with get
    member val UserAction = userAction with get

type BoxPlotControl() as control =
    inherit ChartWithAxisBase()

    let pointHoverEnter = new Event<BoxPlotRegionHoverEventArgs>()
    let pointHoverExit = new Event<BoxPlotRegionHoverEventArgs>()

    let mutable visualRegionMap : List<Visual * (Visual * (int * BoxPlotPoints))> = []
    let mutable lastRegionSelected : (Visual * (int * BoxPlotPoints)) option = None

    let calculateSize () = 
        let width = control.XAxisSize + control.XPadding
        let height = control.YAxisSize + control.YPadding
        Size(width, height)

    let regionWidth points = 
        let pointCount = List.length points
        if pointCount = 0 then
            control.XAxisSize 
        else
            control.XAxisSize / (float pointCount)

    let singleBoxWidth regionWidth points = 
       regionWidth / (float (List.length points + 1))

    let regionVisual regionWidth pointIndex isHighlighted =
        let renderRegion (ctx : DrawingContext) =
            let startP = Point((float pointIndex) * regionWidth + control.XPadding, 0.0)
            let endP = Point(startP.X + regionWidth, startP.Y + control.YAxisSize)
            ctx.DrawRectangle(Theme.boxRegionBrush isHighlighted, null, Rect(startP, endP))    
        let regionDrawing = DrawingUtils.drawing renderRegion
        if not isHighlighted then
            Effect.hideVisual regionDrawing
        regionDrawing

    let boxVisual (onlyRange : bool) (brushMap : FiveNumberSummary<float> -> obj -> Brush) (regionWidth : float) (boxWidth : float) (index : int) (seriesIndex : int) (point : (FiveNumberSummary<float> * obj)) = 
        let toPix f = control.YAxisSize - (f * control.YAxisSize) - control.YPadding
        let fiveN = fst point
        let brush = brushMap fiveN (snd point)
        let w = boxWidth / 2.0
        let medianLineStartPt = Point(0.0 + Theme.boxQuartileRegionPenWidth, toPix fiveN.median)
        let medianLineEndPt = Point(boxWidth - Theme.boxQuartileRegionPenWidth, toPix fiveN.median)

        let minMaxLineStartPt = Point(w, toPix fiveN.minimum)
        let minMaxLineEndPt = Point(w, toPix fiveN.maximum)

        let quartileBoxStartPt = Point(medianLineStartPt.X, toPix fiveN.upperQuartile)
        let quartileBoxEndPt = Point(medianLineEndPt.X, toPix fiveN.lowerQuartile)

        let render (ctx : DrawingContext) =
            if not onlyRange then
                ctx.DrawLine(Theme.boxMinMaxLinePen brush, minMaxLineStartPt, minMaxLineEndPt)
            ctx.DrawRectangle(brush, Theme.boxQuartileRegionPen brush, Rect(quartileBoxStartPt, quartileBoxEndPt))
            if not onlyRange then
                ctx.DrawLine(Theme.boxMedianLinePen, medianLineStartPt, medianLineEndPt)
            
            
        let moveToMainChartAreaTransform : Transform = upcast TranslateTransform(control.XPadding + ((float index) * regionWidth) + (float seriesIndex + 0.5) * boxWidth, control.YPadding)
        DrawingUtils.drawingWithTransform moveToMainChartAreaTransform render

    let boxSeriesVisuals  (onlyRange : bool) (width : float) brushMap (index : int) (point : ChartPoints<FiveNumberSummary<float>>) : (Visual list * (List<Visual * (Visual * (int * BoxPlotPoints))>) * Visual) =
        let boxWidth = singleBoxWidth width point.values
        let region = regionVisual width index point.isHighLighted
        let boxes = List.mapi (boxVisual onlyRange brushMap width boxWidth index) point.values
        let boxToRegionMap = List.map (fun b -> (b, (region, (index, point)))) boxes
        let regionToRegionMap = [(region, (region, (index, point)))]
        boxes, (List.concat [boxToRegionMap; regionToRegionMap]), region

    let boxVisuals (onlyRange : bool) width brushMap (points : ChartPoints<FiveNumberSummary<float>> list) =  
        let bs, maps, regions = List.mapi (boxSeriesVisuals onlyRange width brushMap) points |> List.unzip3
        let boxes = List.concat bs
        let regionMaps = List.concat maps
        boxes, regionMaps, regions

    let chartVisuals (bp : BoxPlot) brushMap  =
        let xAxis = control.XAxisVisuals bp.xAxis
        let yAxis = control.YAxisVisuals bp.yAxis
        let boxes, regionMaps, regions = boxVisuals bp.plotRangeOnly (regionWidth bp.points) brushMap bp.points 
        ((List.concat [xAxis; yAxis; regions; boxes]), regionMaps)
   
    let redraw () = 
        let prov : IBoxPlotDataProvider =  control.BoxPlotDataProvider
        let pointCount = List.length prov.ChartData.points
        let visuals, regionMap = chartVisuals prov.ChartData prov.BrushForDataPoint
        visualRegionMap <- regionMap
        visuals

    let selectEntry entry userAction =
       

        if Option.isSome entry then
            if Option.isSome lastRegionSelected then
                let regionAndData = Option.get lastRegionSelected
                let region, (index, points) = regionAndData
                pointHoverExit.Trigger(BoxPlotRegionHoverEventArgs(index, points, userAction))
                Effect.hideVisual region
            let _, regionAndData = Option.get entry
            let region, (index, points) = regionAndData
            Effect.showVisual region
            pointHoverEnter.Trigger(BoxPlotRegionHoverEventArgs(index, points, userAction))
            lastRegionSelected <- Some regionAndData

    static let BoxPlotDataProviderProperty =
        DependencyProperty.Register(
                                     "BoxPlotDataProvider",
                                     typeof<IBoxPlotDataProvider>,
                                     typeof<BoxPlotControl>,
                                     new PropertyMetadata(
                                        EmptyBoxPlotDataProvider(), 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))

    static let ShowXAxisLabelsProperty =
        DependencyProperty.Register(
                                     "ShowXAxisLabels",
                                     typeof<bool>,
                                     typeof<BoxPlotControl>,
                                     new PropertyMetadata(
                                        true, 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))
    override x.OnVisualMouseEnter v = 
        let entry = List.tryFind (fun (visual, _) -> visual = v) visualRegionMap
        selectEntry entry true
            


    override x.CalculateSize = calculateSize()
    override x.Redraw() = redraw()
    override x.XAxisSize = 800.0
    override x.YAxisSize = 150.0
    override x.IncludeXAxisLabels = x.ShowXAxisLabels
    member x.PointHoverExit = pointHoverExit.Publish
    member x.PointHoverEnter = pointHoverEnter.Publish
    member x.SelectPointByIndex i = 
        let entry = List.tryFind (fun (visual, (_, (idx, _))) -> idx = i) visualRegionMap
        selectEntry entry false

    member x.BoxPlotDataProvider
        with get() = x.GetValue(BoxPlotDataProviderProperty) :?> IBoxPlotDataProvider
        and  set(v : IBoxPlotDataProvider) = x.SetValue(BoxPlotDataProviderProperty, v)
    member x.ShowXAxisLabels
        with get() = x.GetValue(ShowXAxisLabelsProperty) :?> bool
        and  set(v : bool) = x.SetValue(ShowXAxisLabelsProperty, v)
    

