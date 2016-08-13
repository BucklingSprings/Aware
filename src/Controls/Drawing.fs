namespace BucklingSprings.Aware.Controls.Drawing


open System.Windows.Media
open System.Windows

open BucklingSprings.Aware.Common.Themes

module DrawingUtils =

    let drawingWithTransform transform (renderer : DrawingContext -> Unit) : Visual =
        let v = DrawingVisual()
        let ctx = v.RenderOpen()
        renderer  ctx
        ctx.Close()
        v.Transform <- transform
        
        upcast v

    let drawing = drawingWithTransform null



module DrawingContextExtensions =

    type DrawingContext with
        member ctx.DrawTextCenteredAtAndAbove (ft : FormattedText) (pt : Point) =
            let textCorrection = ft.Width / 2.0
            let textPoint = Point(pt.X - textCorrection, pt.Y - Theme.axisLabelPadding - Theme.axisLabelHeight)
            ctx.DrawText(ft, textPoint)

        member ctx.DrawTextCenteredAtAndBelow (ft : FormattedText) (pt : Point) =
            let textCorrection = ft.Width / 2.0
            let textPoint = Point(pt.X - textCorrection, pt.Y + Theme.axisLabelPadding)
            ctx.DrawText(ft, textPoint)

        member ctx.DrawTextBelow (ft : FormattedText) (pt : Point) =
            let textPoint = Point(pt.X, pt.Y + Theme.axisLabelPadding)
            ctx.DrawText(ft, textPoint)

        member ctx.DrawTextLeftOf (ft : FormattedText) (pt : Point) =
            let textCorrection = ft.Width
            let textPoint = Point(pt.X - textCorrection - Theme.axisLabelPadding, pt.Y)
            ctx.DrawText(ft, textPoint)

        member ctx.DrawTextCenteredAtAndLeftOf (ft : FormattedText) (pt : Point) =
            let textCorrection = ft.Width
            let textPoint = Point(pt.X - textCorrection - Theme.axisLabelPadding, pt.Y - (Theme.axisLabelHeight / 2.0))
            ctx.DrawText(ft, textPoint)

        member ctx.DrawLineCenteredAtAndBelow (pt : Point) (length : float) (pen : Pen) =
            let startP = pt
            let endP = Point(pt.X, pt.Y + length)
            ctx.DrawLine(pen, startP, endP)


        member ctx.DrawTextCenteredAt (ft : FormattedText) (pt : Point) =
            let textWidthCorrection = ft.Width / 2.0
            let textHeightCorrection = ft.Height / 2.0
            let textPoint = Point(pt.X - textWidthCorrection, pt.Y - textHeightCorrection)
            ctx.DrawText(ft, textPoint)

