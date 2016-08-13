namespace BucklingSprings.Aware.Controls.Dates


open System.Windows
open System.Windows.Media
open System.Windows.Controls.Primitives

open BucklingSprings.Aware.Common.Themes

type DateRangeTickbar() as tb =
    inherit TickBar()

    override x.OnRender  (dc : DrawingContext) =
        let ht = tb.ActualHeight
        let wd = tb.ActualWidth
        let pointsPerTick = wd / 10.0
        dc.DrawLine(Theme.dateRangeTickPen, Point(0.0, 0.0), Point(0.0, ht))
        dc.DrawLine(Theme.dateRangeTickPen, Point(wd, 0.0), Point(wd, ht))
        for i = 1 to 9 do
            let x = float i * pointsPerTick
            dc.DrawLine(Theme.dateRangeTickPen, Point(x, 0.25 * ht), Point(x, 0.75 * ht))

