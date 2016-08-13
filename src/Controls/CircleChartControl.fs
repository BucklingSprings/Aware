
namespace BucklingSprings.Aware.Controls.Charts

open System.Windows
open System.Windows.Media
open System.Windows.Controls

open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Controls.Media.Effects

type CirclePointData(percentage : float, o : obj) =
    member val Percentage = percentage with get
    member val Object = o with get

type CircleSegmentHoverEventArgs(p : CirclePointData, userAction : bool) =
    inherit System.EventArgs()
    member val Data = p.Object with get
    member val UserAction = userAction with get

[<NoComparison()>]
type PieSliceData = { circleDataPoint: CirclePointData; hoverVisual : Visual}

type CircleChartControl() as control =
    inherit VisualChildrenGeneratingControlBase()

    let mutable visualDetailsMap : List<Visual * PieSliceData> = []
    let pointHoverEnter = new Event<CircleSegmentHoverEventArgs>()

    let width = 200.0
    let widthWithBorder = 200.0 + 16.0
    let hoverRadius = (width / 2.0) +  7.0
    let radius = width / 2.0
    let innerRadius = radius / 2.0
    let center = Point(widthWithBorder/2.0,widthWithBorder/2.0)
    

    let calculateSize () = Size(widthWithBorder,widthWithBorder)

    let polarToCartesian angle length (cartesianoffset : Point) =
        let x = length * System.Math.Cos(angle)
        let y = length * System.Math.Sin(angle)
        let pt = Point(x, y)
        pt.Offset(cartesianoffset.X, cartesianoffset.Y)
        pt

    let toRadians degree = ((degree) * System.Math.PI) / 180.0

    // Trying to fix an issue where ia single class is selected no pie is displayed
    // HACK - FIXME
    let percentageToDegree p = p * 359.99999

    let pieSlice startAngleDegreee wedgeAngleDegree pieInnerRadius pieOuterRadius =

        let startAngle = toRadians (startAngleDegreee - 90.0)
        let wedgeAngle = toRadians wedgeAngleDegree

        let geom = StreamGeometry()
        let ctx = geom.Open()



        let innerArcStartPoint = polarToCartesian startAngle pieInnerRadius center
        let innerArcEndPoint = polarToCartesian (startAngle + wedgeAngle) pieInnerRadius center
        let innerArcSize = Size(pieInnerRadius, pieInnerRadius)

        let outerArcStartPoint = polarToCartesian startAngle pieOuterRadius center
        let outerArcEndPoint = polarToCartesian (startAngle + wedgeAngle) pieOuterRadius center
        let outerArcSize = Size(pieOuterRadius, pieOuterRadius)

        let largeArc = wedgeAngleDegree > 180.0

        ctx.BeginFigure(innerArcStartPoint, true, true)
        ctx.LineTo(outerArcStartPoint, true, true)
        ctx.ArcTo(outerArcEndPoint, outerArcSize, 0.0, largeArc, SweepDirection.Clockwise, true, true)
        ctx.LineTo(innerArcEndPoint, true, true)
        ctx.ArcTo(innerArcStartPoint, innerArcSize, 0.0, largeArc, SweepDirection.Counterclockwise, true, true)
       
        ctx.Close()
        geom :> Geometry

    let dataPieSlice startAngleDegreee wedgeAngleDegree =
        pieSlice startAngleDegreee wedgeAngleDegree innerRadius radius

    let hoverPieSlice startAngleDegreee wedgeAngleDegree =
        pieSlice startAngleDegreee wedgeAngleDegree (hoverRadius-2.0) hoverRadius



    let visualFromGeometry brush geom =
        let render (ctx : DrawingContext) =
            ctx.DrawGeometry(brush, null, geom)
        DrawingUtils.drawing render
    
    let backgroundVisuals =
        [visualFromGeometry Theme.circleChartBaseColorBrush (dataPieSlice 0.0 359.9999)]


    let pieVisualPairs points brushMap =
        let fold previousPoints (percentage : float, o : obj) =
            if List.isEmpty previousPoints then
                [(0.0, percentage, CirclePointData(percentage, o))]
            else
                let lastPointStart,lastPointEnd,_ = List.head previousPoints
                List.Cons((lastPointStart+lastPointEnd, percentage, CirclePointData(percentage, o)), previousPoints)
        let pieVisualAndObj (startPercentage, endPercentage, o : CirclePointData) =
            let geom = dataPieSlice (percentageToDegree startPercentage) (percentageToDegree endPercentage)
            let hoverGeom = hoverPieSlice (percentageToDegree startPercentage) (percentageToDegree endPercentage)
            let brush = brushMap startPercentage o.Object
            (visualFromGeometry brush geom, { circleDataPoint = o; hoverVisual = visualFromGeometry brush hoverGeom})

        points  
            |> List.fold fold []
            |> List.map pieVisualAndObj
            
    let redraw () =
        let prov : ICircleChartDataProvider = control.CircleChartDataProvider
        visualDetailsMap <- pieVisualPairs prov.ChartData.points prov.BrushForDataPoint
        let hoverVisuals = visualDetailsMap |> List.map (snd >> (fun p -> p.hoverVisual))
        hoverVisuals |> List.iter Effect.hideVisual
        List.concat [backgroundVisuals; visualDetailsMap |> List.map fst ; hoverVisuals]

    let selectItem selectedO userAction =
        let vs = visualDetailsMap |> List.map (snd >> (fun r  -> r.hoverVisual))
        if Option.isSome selectedO then
            List.iter (fun v -> Effect.hideVisual v) vs
            let v,data = Option.get selectedO
            Effect.showVisual data.hoverVisual
            pointHoverEnter.Trigger(CircleSegmentHoverEventArgs(data.circleDataPoint, userAction))

    static let CircleChartDataProviderProperty =
        DependencyProperty.Register(
                                     "CircleChartDataProvider",
                                     typeof<ICircleChartDataProvider>,
                                     typeof<CircleChartControl>,
                                     new PropertyMetadata(
                                        EmptyCircleChartDataProvider(), 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))
    override x.Redraw () = redraw()
    override x.CalculateSize = calculateSize()
    override x.OnVisualMouseEnter v =
        let selected = List.tryFind (fun (visual, _) -> visual = v) visualDetailsMap
        selectItem selected true
            
    member x.SelectByPredicate (pred : obj -> bool) =
        let selected = visualDetailsMap |> List.tryFind (fun (_v, p) -> pred p.circleDataPoint.Object)
        selectItem selected false

    member x.PointHoverEnter = pointHoverEnter.Publish
    member x.CircleChartDataProvider
        with get() = x.GetValue(CircleChartDataProviderProperty) :?> ICircleChartDataProvider
        and  set(v : ICircleChartDataProvider) = x.SetValue(CircleChartDataProviderProperty, v)
    

