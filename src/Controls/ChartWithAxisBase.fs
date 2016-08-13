namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions

open System.Windows
open System.Windows.Media


[<AbstractClass()>]
type ChartWithAxisBase() as control =
    inherit VisualChildrenGeneratingControlBase()

    let xAxisLabels (axisPoints : ChartAxisPoint list) =
        let labelVisual (pt : ChartAxisPoint) =
            let label = Theme.axisLabelFormattedText pt.humanized
            let render (ctx : DrawingContext) =
                let startPoint = Point((pt.value * control.XAxisSize) + control.XPadding, 0.0)
                let endPoint = Point((pt.value * control.XAxisSize) + control.XPadding, control.YAxisSize)
                // Never draw a guide line to close to the edges
                if pt.value > 0.001 then
                    ctx.DrawLine(Theme.axisPen, startPoint, endPoint)
                if control.IncludeXAxisLabels then
                    ctx.DrawTextBelow label endPoint
            DrawingUtils.drawing render
        List.map labelVisual axisPoints


    let xAxisVisuals (axisPoints : ChartAxisPoint list) =
        let startP = Point(control.XPadding, control.YAxisSize)
        let endP = Point(control.XPadding + control.XAxisSize, control.YAxisSize)
        let render (ctx : DrawingContext) =
            ctx.DrawLine(Theme.horizontalGuidePen, startP, endP)
        List.Cons(DrawingUtils.drawing render, xAxisLabels axisPoints)


    let yAxisVisuals (axisPoints : ChartAxisPoint list) =
        let render (ctx : DrawingContext) =
            let drawLabel (p : ChartAxisPoint) =
                let ft = Theme.axisLabelFormattedText p.humanized
                let startPoint = Point(control.XPadding, (1.0 - p.value) * control.YAxisSize)
                let endPoint = Point(control.XPadding + control.XAxisSize,  startPoint.Y)
                ctx.DrawLine(Theme.horizontalGuidePen, startPoint, endPoint)
                ctx.DrawTextLeftOf ft startPoint
            List.iter drawLabel axisPoints
        [DrawingUtils.drawing render]

    abstract YAxisSize : float
    abstract XAxisSize : float
    abstract IncludeXAxisLabels : bool
    member x.XPadding = 50.0
    member x.YPadding = if x.IncludeXAxisLabels then 50.0 else 15.0
    member x.XAxisVisuals points = xAxisVisuals points
    member x.YAxisVisuals points = yAxisVisuals points

