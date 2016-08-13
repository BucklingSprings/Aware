namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions
open BucklingSprings.Aware.Controls.Media.Effects

open System.Windows
open System.Windows.Media

[<NoComparison()>]
type RegionMapEntry = {
    visual : Visual; 
    data : obj list; 
    day : TimeSeriesPhantom.TimeSeriesPeriod<TimeSeriesPhantom.TimeSeriesPeriodDayOfWeek>;
    hour : TimeSeriesPhantom.TimeSeriesPeriod<TimeSeriesPhantom.TimeSeriesPeriodHourOfDay>}

type DayHourMatrixControl() as ctl =
    inherit VisualChildrenGeneratingControlBase()

    let showDetailEvent = new Event<TimeSeriesPhantom.TimeSeriesPeriod<TimeSeriesPhantom.TimeSeriesPeriodDayOfWeek> *  TimeSeriesPhantom.TimeSeriesPeriod<TimeSeriesPhantom.TimeSeriesPeriodHourOfDay> * obj list>()   
    let paddingLeft = 78.0
    let paddingBottom = 30.0
    let pixelsPerHour = 30.0
    let dayHeight = 56.0
    let width = 24.0 * pixelsPerHour
    let height = 7.0 * dayHeight

    let mutable regionMap : RegionMapEntry list = []
    let mutable lastSelectedRegionEntry : RegionMapEntry option = None

    let hourLabelVisuals =
        let lbls = [for i in 3..3..23-> i] |> List.map (fun h -> (float h, h |> Humanize.hoursFromStartOfDayToTimeFormat |> Theme.axisLabelFormattedText))
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(paddingLeft, 0.0))
            (fun ctx ->
                        let place (index, label) =
                            let tickPoint  = Point(index * pixelsPerHour, height)
                            ctx.DrawTextCenteredAtAndBelow label tickPoint
                        List.iter place lbls)]

    let hourGuideVisuals =
        let guideIndices = [for i in 0..3..24-> float i]
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(paddingLeft, 0.0))
            (fun ctx ->
                        let placeGuideLine index =
                            let x = index * pixelsPerHour
                            let startP = Point(x, 0.0)
                            let endP = Point(x, height)
                            ctx.DrawLine(Theme.axisPen, startP, endP)
                        List.iter placeGuideLine guideIndices)]

    let dayLabelVisuals =
        let lbls =  [for i in 0..6-> Humanize.dayOfWeek i]
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(0.0, 0.0))
            (fun ctx ->
                let place index label =
                    let tickPt = Point(paddingLeft, (float index) * dayHeight + dayHeight / 2.0)
                    let ft = Theme.axisLabelFormattedText label
                    ctx.DrawTextCenteredAtAndLeftOf ft tickPt
                List.iteri place lbls)]

    let dayGuideVisuals =
        let guideIndices = [for i in 0..7-> float i]
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(paddingLeft, 0.0))
            (fun ctx ->
                let placeGuideLine index =
                    let y = index * dayHeight
                    let startP = Point(0.0, y)
                    let endP = Point(width, y)
                    ctx.DrawLine(Theme.horizontalGuidePen, startP, endP)
                List.iter placeGuideLine guideIndices)]

    let backgroundVisuals =
        let backgroundIndices = [for i in 0..6-> i]
        DrawingUtils.drawingWithTransform
            (TranslateTransform(paddingLeft, 0.0))
            (fun ctx ->
                let backgroundForDay i =
                    let index = float i
                    let startP = Point(0.0, index * dayHeight)
                    let endP = Point(width, startP.Y + dayHeight)
                    let brush = if i % 2 = 0 then Theme.dayHourMatrixEvenBackground else Theme.dayHourMatrixOddBackground
                    ctx.DrawRectangle(brush, null, Rect(startP, endP))
                List.iter backgroundForDay backgroundIndices
            )

  
    let regionVisuals dayindex dayPeriod hourIndex hourPeriod (points : DayHourMatrixHourData list) : RegionMapEntry  =
        let segmentCorrection = (float hourIndex) * pixelsPerHour
        let dayCorrection =  (float) dayindex* dayHeight
        let background = DrawingUtils.drawingWithTransform
                            (TranslateTransform(paddingLeft + segmentCorrection, dayCorrection))
                            (fun ctx ->
                                let startPt = Point(0.0, 0.0)
                                let endPt = Point(pixelsPerHour, dayHeight)
                                ctx.DrawRectangle(Theme.barRegionBrush, null, Rect(startPt, endPt))
                                )
        Effect.hideVisual background
        let xs = points |> List.map (fun f -> f.data)
        {RegionMapEntry.data = xs; RegionMapEntry.visual = background; day = dayPeriod; hour = hourPeriod}

    let segment dayIndex segmentIndex (segmentStart : DayHourMatrixHourData, segmentEnd : DayHourMatrixHourData) =
        let segmentCorrection = (float segmentIndex + 0.5) * pixelsPerHour
        let dayCorrection = (float dayIndex) * dayHeight
        DrawingUtils.drawingWithTransform
            (TranslateTransform(paddingLeft + segmentCorrection, dayCorrection))
            (fun ctx ->
                let startPt = Point(0.0, dayHeight - (segmentStart.value * dayHeight))
                let endPt = Point(pixelsPerHour, dayHeight - (segmentEnd.value * dayHeight))
                let p = Theme.trendSegmentPen segmentStart.brush
                ctx.DrawLine(p, startPt, endPt)
                ctx.DrawEllipse(Brushes.White, p, startPt, 3.0, 3.0)
                ctx.DrawEllipse(null,Theme.trendPointPen Brushes.White, startPt, 5.0, 5.0)
                if segmentIndex = 22 then
                    ctx.DrawEllipse(Brushes.White, p, endPt, 3.0, 3.0)
                    ctx.DrawEllipse(null,Theme.trendPointPen Brushes.White, endPt, 5.0, 5.0)
                )

     
    let dayPlot (dayIndex : int) (ts : DayHourMatrixDayData) =
        let values = TimeSeriesPhantom.TimeSeries.extractData id ts
        let pairs = values |> Seq.pairwise |> Seq.map (fun (x, y) -> List.zip x y) |> Seq.toList
        pairs |> List.mapi (fun segmentIndex ->  List.map (segment dayIndex segmentIndex)) |> List.concat
       
    

    let redraw () =
        let prov : IDayHourMatrixProvider = ctl.DayHourMatrixProvider
        let data = prov.DayHourMatrix
        let dailyPlots = data |> TimeSeriesPhantom.TimeSeries.extractDatai dayPlot |> List.concat
        regionMap <- List.concat ( TimeSeriesPhantom.TimeSeries.extractDataAndPeriodi (fun dayIndex p -> TimeSeriesPhantom.TimeSeries.extractDataAndPeriodi (regionVisuals dayIndex p))  data)
        let regionVisuals = regionMap |> List.map (fun (r : RegionMapEntry) -> r.visual)
        List.concat [[backgroundVisuals]; hourLabelVisuals; hourGuideVisuals; dayLabelVisuals; dayGuideVisuals; regionVisuals; dailyPlots]
 
    
    static let DayHourMatrixProviderProperty =
        DependencyProperty.Register(
            "DayHourMatrixProvider", 
            typeof<IDayHourMatrixProvider>, 
            typeof<DayHourMatrixControl>, 
            PropertyMetadata(
                EmptyDayHourMatrixProvider(),
                PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)
        ))

    override x.Redraw () = redraw()
    override x.CalculateSize = Size(paddingLeft + width, height + paddingBottom)

    override x.OnVisualMouseEnter v = 
        
        let r = List.tryFind (fun (r : RegionMapEntry) -> r.visual.GetHashCode() = v.GetHashCode()) regionMap
        if Option.isSome r then
            let regionEntry = Option.get r

            if Option.isSome lastSelectedRegionEntry then
                let lastEntry = Option.get lastSelectedRegionEntry
                Effect.hideVisual lastEntry.visual
            showDetailEvent.Trigger (regionEntry.day, regionEntry.hour, regionEntry.data)
            
            
            Effect.showVisual regionEntry.visual
            lastSelectedRegionEntry <- Some regionEntry

    member x.ShowDetail = showDetailEvent.Publish
    member x.DayHourMatrixProvider
        with get() = x.GetValue(DayHourMatrixProviderProperty) :?> IDayHourMatrixProvider
        and  set(v : IDayHourMatrixProvider) = x.SetValue(DayHourMatrixProviderProperty, v)
