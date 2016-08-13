namespace BucklingSprings.Aware.Common.Themes


open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Media.Effects

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement

[<NoComparison()>]
type AssignedBrushes = {fore: Brush; back : Brush}


module Theme =
    
    let fromBrushes bColor fColor =
        let f = SolidColorBrush(fColor) :> Brush
        let b = SolidColorBrush(bColor) :> Brush
        {fore = f; back = b}
    let fromBrushesWhiteForeground bColor = fromBrushes bColor Colors.White
    
    let awareColor = Color.FromRgb(58uy, 173uy, 217uy)
    let awareBrush = SolidColorBrush(awareColor)
    let awareAssignedBrushes = fromBrushesWhiteForeground awareColor
    let labelBrush = SolidColorBrush(Color.FromRgb(128uy, 128uy, 128uy))
    let axisBrush = SolidColorBrush(Color.FromRgb(212uy, 212uy, 212uy))
    
    

    
    let totalColors = fromBrushesWhiteForeground awareColor
    let otherColors = fromBrushesWhiteForeground (Color.FromRgb(125uy, 125uy, 125uy))
    let idleColors = fromBrushesWhiteForeground Colors.DarkGray
    let offColors = fromBrushesWhiteForeground Colors.White
    let customColors =
        List.map fromBrushesWhiteForeground [Color.FromRgb(223uy, 107uy, 50uy)
                                             Color.FromRgb(191uy, 203uy, 67uy)
                                             Color.FromRgb(112uy, 93uy, 149uy)
                                             Color.FromRgb(106uy, 197uy, 101uy)
                                             Color.FromRgb(4uy, 104uy, 138uy)
                                             Color.FromRgb(204uy, 83uy, 56uy)]

    let dayLengthStartTimeColors = fromBrushesWhiteForeground Colors.Green
    let dayLengthEndTimeColors = fromBrushesWhiteForeground Colors.Red
    let unknownColors = fromBrushesWhiteForeground Colors.Yellow

    let infiniteCustomColors =
        seq {
            while true do
                yield! customColors
        }
        

    let genericFormattedText brush s = 
        let ff = FontFamily(System.Uri("pack://application:,,,/"), "/BucklingSprings.Aware.Common;Component/#Lato Bold")
        let font = Typeface(ff, FontStyles.Normal, FontWeights.Regular, FontStretches.Expanded)
        FormattedText(s, System.Globalization.CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, font,10.0, brush)

    let scatterPlotPointRadius = 2.0

    let axisThickness = 1.0

    let axisPen = Pen(axisBrush, axisThickness)

    let horizontalGuidePen = 
        let p = Pen(axisBrush, axisThickness)
        p.DashStyle <- DashStyle([| 4.0; 4.0|], 0.0)
        p
    
    let axisLabelFormattedText = genericFormattedText labelBrush

    let axisLabelHeight = axisLabelFormattedText("Some random String").Height

    let axisLabelPadding = axisLabelHeight

    let barRegionBrush = axisBrush

    let barHoverEffect = DropShadowEffect(ShadowDepth = 0.0, BlurRadius = 4.0)

    
    let hoverGuideBrush = axisBrush

    let hoverGuideLinePen = Pen(hoverGuideBrush, 1.0)

    let hoverGuidePointPen = Pen(hoverGuideBrush, 1.0)

    let hoverGuidePointRadius = 2.0


    let trendSegmentPen brush = Pen(brush, 2.0)

    let trendPointPen brush = Pen(brush, 3.0)

    let scatterPointPen brush = Pen(brush, 0.5)

    let boxMedianLinePen = Pen(Brushes.White, 2.0)
    let boxMinMaxLinePen brush = Pen(brush, 2.0, DashStyle = DashStyles.Dash)
    let boxQuartileRegionPenWidth = 2.0
    let boxQuartileRegionPen brush = Pen(brush, boxQuartileRegionPenWidth)
    let boxRegionBrush isHighLighted = if isHighLighted then Brushes.LightBlue else Brushes.LightGray

    let dayHourMatrixEvenBackground = SolidColorBrush(Color.FromRgb(237uy, 237uy, 237uy))
    let dayHourMatrixOddBackground = SolidColorBrush(Color.FromRgb(255uy, 255uy, 255uy))
    

    let timeMapBackground = 
        let w = 7.0
        let geom = LineGeometry(Point(w, 0.0), Point(0.0, w))
        let p = Pen(axisBrush, 1.0)
        let drw = GeometryDrawing(null, axisPen, geom)
        let drawingBrush = DrawingBrush(drw)
        drawingBrush.TileMode <- TileMode.Tile
        drawingBrush.Viewport <- Rect(Size(w, w))
        drawingBrush.ViewportUnits <- BrushMappingMode.Absolute        
        drawingBrush

        


    let circleChartBaseColorBrush = awareBrush

    let dateRangeTickPen = Pen(SolidColorBrush(Color.FromRgb(237uy, 237uy, 237uy)), 1.0)

    let brushByClass (colorMap : ClassIdentifier -> AssignedBrushes) bc : Brush =
        match bc with
        | MeasureByClass.TotalAcrossClasses _ -> upcast awareBrush
        | MeasureByClass.ForClass(c, _) -> (colorMap c).back
