namespace BucklingSprings.Aware.Controls.Charts


open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Charts
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls.Media.Effects

open System.Windows
open System.Windows.Media

type ScalarSeriesChartRegionHoverEventArgs(index : int, points : ScalarChartPoints, userAction : bool) =
    inherit System.EventArgs()
    member val PointIndex = index with get
    member val ScalarValuePoints = points with get
    member val UserAction = userAction with get


type ScalarSeriesChartControl() as control =
    inherit ChartWithAxisBase()

    let pointHoverEnter = new Event<ScalarSeriesChartRegionHoverEventArgs>()
    let pointHoverExit = new Event<ScalarSeriesChartRegionHoverEventArgs>()

    let mutable barRegionMap : List<Visual * (Visual * (int * ScalarChartPoints))> = []
    let mutable lastRegionAndPointSelected : (Visual * (int * ScalarChartPoints)) option = None

    let numberOfScalarValuesPerRegion (bc : ScalarSeriesChart) =
        if List.isEmpty bc.points then
            0.0
        else
            // FIXME TODO Add debug only sanity check to make sure this assumption is valid.
            (List.head bc.points).values.Length |> float

    let regionWidth (bc : ScalarSeriesChart) = 
        let numberOfPoints = float (List.length bc.points)
        control.XAxisSize / numberOfPoints

    let singleScalarValueWidth (bc : ScalarSeriesChart) =
        match bc.displayType with
        | ScalarSeriesDisplayType.Bars -> (regionWidth bc) / ((numberOfScalarValuesPerRegion bc) + 1.0)
        | ScalarSeriesDisplayType.TrendLine -> (regionWidth bc)

    let individualScalarValuePadding (bc : ScalarSeriesChart) =
        match bc.displayType with
        | ScalarSeriesDisplayType.Bars -> (singleScalarValueWidth bc) / 2.0
        | ScalarSeriesDisplayType.TrendLine -> (singleScalarValueWidth bc) / 2.0
        
        
    let calculateSize () = 
        let width = control.XAxisSize + control.XPadding
        let height = control.YAxisSize + control.YPadding
        Size(width, height)

    let regionVisual regionWidth regionHeight pointIndex isHighlighted =
        let renderRegion (ctx : DrawingContext) =
            let startP = Point((float pointIndex) * regionWidth + control.XPadding, 0.0)
            let endP = Point(startP.X + regionWidth, startP.Y + regionHeight)
            ctx.DrawRectangle(Theme.barRegionBrush, null, Rect(startP, endP))    
        let regionDrawing = DrawingUtils.drawing renderRegion
        Effect.hideVisual regionDrawing
        regionDrawing

    let trendVisuals (bc : ScalarSeriesChart) brushDataMap =
        let singleRegionWidth = regionWidth bc
        let scalarValuePadding = individualScalarValuePadding bc
        let pairs = bc.points |> Seq.pairwise |> Seq.map (fun (x, y) -> 
                                                                List.zip x.values y.values) |> Seq.toList
        let pointCount = List.length bc.points
        let segmentVisual (pointIndex : int) (valueIndex : int) (points : ((float * obj) * (float * obj))) = 
            let valueA = fst points
            let valueB = snd points
            let vA = fst valueA
            let dA = snd valueA
            let vB = fst valueB
            let pIndex = float pointIndex
            let segementStartPoint = Point((pIndex * singleRegionWidth) + scalarValuePadding + control.XPadding, control.YAxisSize - (vA * control.YAxisSize))
            let segementEndPoint = Point(segementStartPoint.X + singleRegionWidth, control.YAxisSize - (vB * control.YAxisSize))
            let render (ctx : DrawingContext) =
                let pen = Theme.trendSegmentPen (brushDataMap vA dA)
                let pointPen = Theme.trendPointPen (brushDataMap vA dA)
                ctx.DrawLine(pen, segementStartPoint, segementEndPoint)
                ctx.DrawEllipse(Brushes.White, pointPen, segementStartPoint, 4.0, 4.0)
                ctx.DrawEllipse(null,Theme.trendPointPen Brushes.White, segementStartPoint, 6.0, 6.0)
                if pointIndex = pointCount - 2 then
                    ctx.DrawEllipse(Brushes.White, pointPen, segementEndPoint, 4.0, 4.0)
                    ctx.DrawEllipse(null,Theme.trendPointPen Brushes.White, segementEndPoint, 6.0, 6.0)


            DrawingUtils.drawing render
        let allSegments (pointIndex : int) (points : ((float * obj) * (float * obj)) list) =
            List.mapi (segmentVisual pointIndex) points
        let segments = List.mapi allSegments pairs |> List.concat |> List.map (fun x -> (x, None))
        let regionMap pointIndex (scalarPoints : ScalarChartPoints) =
            let regionDrawing = regionVisual singleRegionWidth control.YAxisSize pointIndex scalarPoints.isHighLighted
            (regionDrawing, Some (regionDrawing, (pointIndex, scalarPoints)))
        let regions = bc.points |> List.mapi regionMap
        List.concat [regions; segments]

    
    
    let barVisuals (bc : ScalarSeriesChart) brushDataMap =
        let singleRegionWidth = regionWidth bc
        let barWidth = singleScalarValueWidth bc
        let scalarValuePadding = individualScalarValuePadding bc
        let barVisual (pointIndex : int) (valueIndex : int) (value : float * obj) =
            let v =  fst value
            let d = snd value
            let pIndex = float pointIndex
            let vIndex = float valueIndex
            let startP = Point((pIndex * singleRegionWidth) + (vIndex * barWidth) + scalarValuePadding + control.XPadding, control.YAxisSize - (v * control.YAxisSize))
            let endP = Point(startP.X + barWidth, control.YAxisSize)
            let render (ctx : DrawingContext) =
                ctx.DrawRectangle(brushDataMap v d, null, Rect(startP, endP))
            DrawingUtils.drawing render
            
        let allBarsVisual (pointIndex : int) (pt : ScalarChartPoints) =
            let regionDrw = regionVisual singleRegionWidth control.YAxisSize pointIndex pt.isHighLighted
            let bars = List.mapi (barVisual pointIndex) pt.values |> List.map (fun b -> (b, Some (regionDrw, (pointIndex, pt))))
            List.concat [[(regionDrw, Some (regionDrw, (pointIndex, pt)))]; bars]

        List.mapi allBarsVisual bc.points |> List.concat

        

    let chartVisuals (bc : ScalarSeriesChart) (brushDataMap : float -> obj -> Brush) =
        let xAxis = control.XAxisVisuals bc.xAxis
        let yAxis = control.YAxisVisuals bc.yAxis
        let pointAndRegionVisual = 
            match bc.displayType with
            | ScalarSeriesDisplayType.Bars -> barVisuals bc brushDataMap
            | ScalarSeriesDisplayType.TrendLine -> trendVisuals bc brushDataMap

        let pointVisuals = List.map fst pointAndRegionVisual
        let visualsMappedToVisuals = pointAndRegionVisual |> List.filter (fun (b, r) -> r <> None) 
        let hoverMap = visualsMappedToVisuals |> List.map (fun (b, r) -> (b, Option.get r))
        (List.concat [xAxis; yAxis; pointVisuals], hoverMap)

    let redraw () = 
        let prov : IScalarSeriesDataProvider =  control.BarChartDataProvider


        let bc = prov.ChartData
        let visuals, brMap = chartVisuals bc prov.BrushForDataPoint
        barRegionMap <- brMap
        visuals

    let hideLastPoint userAction =
        if Option.isSome lastRegionAndPointSelected then
            let region, (index, points) = Option.get lastRegionAndPointSelected
            Effect.hideVisual region
            pointHoverExit.Trigger(ScalarSeriesChartRegionHoverEventArgs(index, points, userAction))
            ()

    let selectPoint (regionVisualAndPoint : (Visual * (int * ScalarChartPoints))) userAction : unit =
        hideLastPoint userAction
        let region, (index, points) = regionVisualAndPoint
        Effect.showVisual region
        pointHoverEnter.Trigger(ScalarSeriesChartRegionHoverEventArgs(index, points, userAction))
        lastRegionAndPointSelected <- Some regionVisualAndPoint
        ()
    static let BarChartDataProviderProperty =
        DependencyProperty.Register(
                                     "BarChartDataProvider",
                                     typeof<IScalarSeriesDataProvider>,
                                     typeof<ScalarSeriesChartControl>,
                                     new PropertyMetadata(
                                        EmptyScalarSeriesDataProvider(), 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))
    static let ShowXAxisLabelsProperty =
        DependencyProperty.Register(
                                     "ShowXAxisLabels",
                                     typeof<bool>,
                                     typeof<ScalarSeriesChartControl>,
                                     new PropertyMetadata(
                                        true, 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))
    override x.OnVisualMouseEnter v = 
        let barAndRegion = List.tryFind (fun (bar, region) -> bar = v) barRegionMap
        if Option.isSome barAndRegion then
            let _, regionVisualAndPoint = Option.get barAndRegion
            selectPoint regionVisualAndPoint true
            


            

    override x.CalculateSize = calculateSize()
    override x.Redraw() = redraw()
    override x.XAxisSize = 500.0
    override x.YAxisSize = 140.0
    override x.IncludeXAxisLabels = x.ShowXAxisLabels
    member x.PointHoverExit = pointHoverExit.Publish
    member x.PointHoverEnter = pointHoverEnter.Publish
    
    member x.SelectPointByIndex i = 
        let barAndRegion = List.tryFind (fun (bar, (region, (index, pt))) -> index = i) barRegionMap
        if Option.isSome barAndRegion then
            let _, regionVisualAndPoint = Option.get barAndRegion
            selectPoint regionVisualAndPoint false
        ()
    member x.BarChartDataProvider
        with get() = x.GetValue(BarChartDataProviderProperty) :?> IScalarSeriesDataProvider
        and  set(v : IScalarSeriesDataProvider) = x.SetValue(BarChartDataProviderProperty, v)
    member x.ShowXAxisLabels
        with get() = x.GetValue(ShowXAxisLabelsProperty) :?> bool
        and  set(v : bool) = x.SetValue(ShowXAxisLabelsProperty, v)
    

