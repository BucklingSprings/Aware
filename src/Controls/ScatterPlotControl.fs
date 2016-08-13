namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls.Media.Effects

open System.Windows
open System.Windows.Media

type ScatterPlotControl() as control =
    inherit VisualChildrenGeneratingControlBase()

    let xPadding = Theme.axisLabelHeight * 8.0
    let yPadding = Theme.axisLabelHeight * 8.0

    let calculateSize () = 
        let prov : IScatterPlotDataProvider =  control.ScatterPlotDataProvider
        let cd = prov.ChartData
        let width = control.XAxisSize + Theme.axisThickness + xPadding
        let height = control.YAxisSize + Theme.axisThickness + yPadding + yPadding
        Size(width, height)

    
    let xAxisVisuals (cd:ScatterPlotChart) =
        let axisWidth = control.XAxisSize + Theme.axisThickness
        let startP = Point(xPadding, control.YAxisSize + Theme.axisThickness + yPadding)
        let endP = Point(xPadding + axisWidth, startP.Y)
        let render (ctx : DrawingContext) =
            ctx.DrawLine(Theme.axisPen, startP, endP)
            let drawLabel (pt : ChartAxisPoint) =
                let ft = Theme.axisLabelFormattedText pt.humanized
                let x = pt.value * control.XAxisSize + xPadding + Theme.axisThickness
                let y = control.YAxisSize + yPadding + Theme.axisThickness
                ctx.DrawTextCenteredAtAndBelow ft (Point(x, y))
                ctx.DrawLineCenteredAtAndBelow (Point(x, y)) 2.0 Theme.axisPen
            List.iter drawLabel cd.xAxis
        [DrawingUtils.drawing render]
       
    let yAxisVisuals (cd : ScatterPlotChart) =
        let startP = Point(xPadding, yPadding)
        let endP = Point(xPadding, control.YAxisSize + Theme.axisThickness + yPadding)
        let render (ctx : DrawingContext) =
            ctx.DrawLine(Theme.axisPen, startP, endP)
            let drawLabel (pt : ChartAxisPoint) =
                let ft = Theme.axisLabelFormattedText pt.humanized
                let x = xPadding - Theme.axisThickness
                let y = ((1.0 - pt.value) * control.YAxisSize) + yPadding
                ctx.DrawTextCenteredAtAndLeftOf ft (Point(x, y))
            List.iter drawLabel cd.yAxis
        [DrawingUtils.drawing render]
           
    let pointVisuals (cd: ScatterPlotChart) (seriesBrushMap: int -> Brush) =
        let render (ctx : DrawingContext) =
            let drawPoint (pt: ScatterPlotPoint) =
                let x = xPadding + Theme.axisThickness + (pt.x * control.XAxisSize)
                let y = ((1.0 - pt.y) * control.YAxisSize) + yPadding
                ctx.DrawEllipse(null, Theme.scatterPointPen (seriesBrushMap pt.index), Point(x,y), Theme.scatterPlotPointRadius, Theme.scatterPlotPointRadius)
            List.iter drawPoint cd.points

        [DrawingUtils.drawing render]
           
    let scatterPlotVisuals cd seriesBrushMap =
        let xAxis = xAxisVisuals cd
        let yAxis = yAxisVisuals cd
        let pv = pointVisuals cd seriesBrushMap
        List.concat [xAxis;yAxis;pv]
              
    let redraw () = 
        let prov : IScatterPlotDataProvider =  control.ScatterPlotDataProvider
        let cd = prov.ChartData
        let visuals = scatterPlotVisuals cd prov.BrushForSeriesByIndex
        visuals

    static let ScatterPlotProviderProperty =
        DependencyProperty.Register(
                                     "ScatterPlotDataProvider",
                                     typeof<IScatterPlotDataProvider>,
                                     typeof<ScatterPlotControl>,
                                     new PropertyMetadata(
                                        EmptyScatterPlotDataProvider(), 
                                        new PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)))

    override x.CalculateSize = calculateSize()
    override x.Redraw() = redraw()
    member val XAxisSize = 600.0 with get, set
    member val YAxisSize = 600.0 with get, set
    member x.ScatterPlotDataProvider
        with get() = x.GetValue(ScatterPlotProviderProperty) :?> IScatterPlotDataProvider
        and  set(v : IScatterPlotDataProvider) = x.SetValue(ScatterPlotProviderProperty, v)