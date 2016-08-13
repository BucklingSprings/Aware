namespace BucklingSprings.Aware.Controls.Charts

open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.TimeSeriesPhantom
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls
open BucklingSprings.Aware.Controls.Drawing
open BucklingSprings.Aware.Controls.Drawing.DrawingContextExtensions

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media

type TimeMapEntry = TimeSeriesPeriod<TimeSeriesPeriodDaily> *  TimeSeriesPeriod<TimeSeriesPeriodTimeOfDay> * MeasureForClass<ActivitySummary>

type TimeMapControl() as cc =
    inherit VisualChildrenGeneratingControlBase()

    let showDetailEvent = new Event<TimeMapEntry>()    
    let pixelsPerMinute = 0.5
    let mapAreaHeight = 392.0 // A Number divisible by both 7
    let mapAreaWidth = 24.0 * pixelsPerMinute * 60.0
    let padding = 73.0;
    let timeMapSize = Size(mapAreaWidth, mapAreaHeight)
    let sz = Size(mapAreaWidth + padding, mapAreaHeight + padding)
    let mutable blockDetailMap : List<Visual * TimeMapEntry> = []

    let calculateDayHeight (tm : TimeMap) = mapAreaHeight / float(TimeSeries.length tm)

    let hourLabelVisuals =
        let lbls = [for i in 3..3..23-> i] |> List.map (fun h -> (float h, h |> Humanize.hoursFromStartOfDayToTimeFormat |> Theme.axisLabelFormattedText))
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(padding, 0.0))
            (fun ctx ->
                        let place (index, label) =
                            let tickPoint  = Point(index * 60.0 * pixelsPerMinute, mapAreaHeight)
                            ctx.DrawTextCenteredAtAndBelow label tickPoint
                        List.iter place lbls)]

    let hourGuideVisuals =
        let guideIndices = [for i in 0..3..24-> float i]
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(padding, 0.0))
            (fun ctx ->
                        let placeGuideLine index =
                            let x = index * 60.0 * pixelsPerMinute
                            let startP = Point(x, 0.0)
                            let endP = Point(x, mapAreaHeight)
                            ctx.DrawLine(Theme.axisPen, startP, endP)
                        List.iter placeGuideLine guideIndices)]


    let backgroundVisual =
        DrawingUtils.drawingWithTransform
            (TranslateTransform(padding, 0.0))
            (fun ctx ->
                let startP = Point(0.0, 0.0)
                let endP = Point(mapAreaWidth, mapAreaHeight)
                ctx.DrawRectangle(Theme.timeMapBackground, null, Rect(startP, endP))
                    )

    
    let dayLabelVisuals (tm : TimeMap) =
        let dayHeight =  calculateDayHeight tm
        let lbls = TimeSeries.extractPeriods TimeSeriesPeriods.humanize tm
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(0.0, 0.0))
            (fun ctx ->
                let place index label =
                    let tickPt = Point(padding, (float index) * dayHeight + dayHeight / 2.0)
                    let ft = Theme.axisLabelFormattedText label
                    ctx.DrawTextCenteredAtAndLeftOf ft tickPt
                List.iteri place lbls)]

    let dayGuideVisuals (tm : TimeMap) =
        let dayHeight =  calculateDayHeight tm
        let guideIndices = [for i in 0..(TimeSeries.length tm)-> float i]
        [DrawingUtils.drawingWithTransform
            (TranslateTransform(padding, 0.0))
            (fun ctx ->
                let placeGuideLine index =
                    let y = index * dayHeight
                    let startP = Point(0.0, y)
                    let endP = Point(mapAreaWidth, y)
                    ctx.DrawLine(Theme.horizontalGuidePen, startP, endP)
                List.iter placeGuideLine guideIndices)]


    let dayVisuals brushMap dayHeight dayCount transform dayIndex dayPeriod (dayMap : DayMap) =
        let dayIndexf = float dayIndex
        let lastDay = dayIndex = dayCount - 1
        let blockVisual index period (b : MeasureForClass<ActivitySummary>) = 
            //'let mins (d : DateTimeOffset)  = let ts = d - d.StartOfDay in ts.TotalMinutes
            let mins p = let ts = TimeSeriesPeriodConversions.toTimeSpan p in ts.TotalMinutes
            let breathingRoomY = 3.0
            let breathingRoomX = 0.0
            let classId, summ = b
            let leftTopCorner = Point((mins period) * pixelsPerMinute + breathingRoomX, dayHeight * dayIndexf + breathingRoomY)
            let rightBottomCorner = Point(((mins period) +  (float summ.minuteCount) ) * pixelsPerMinute - breathingRoomX, (dayHeight * dayIndexf) + dayHeight - breathingRoomY)                                        
            let colors : AssignedBrushes = brushMap(fst b)
            (DrawingUtils.drawingWithTransform transform (fun c -> c.DrawRectangle(colors.back, null, Rect(leftTopCorner, rightBottomCorner))), (dayPeriod, period, b))
        TimeSeries.extractDataAndPeriodi blockVisual dayMap


    let timeMapVisuals brushMap (tm : TimeMap)  =
        let dayCount = TimeSeries.length tm
        let dayHeight =  calculateDayHeight tm
        let transform = TranslateTransform(padding, 0.0) :> Transform
        let dayBarVisualsAndDescription = TimeSeries.extractDataAndPeriodi (dayVisuals brushMap dayHeight dayCount transform) tm |> List.concat
        let dayBarVisuals = dayBarVisualsAndDescription |> List.map fst
        (dayBarVisualsAndDescription, List.concat [[backgroundVisual] ; dayLabelVisuals tm; dayGuideVisuals tm ; hourLabelVisuals;  hourGuideVisuals; dayBarVisuals;])


    let redraw () = 
        let prov : ITimeMapDataProvider = cc.TimeMapDataProvider
        let tm = prov.TimeMap
        let  vMap, visuals = timeMapVisuals prov.BrushesForClass tm
        blockDetailMap <- vMap
        visuals


    static let TimeMapDataProviderProperty =
        DependencyProperty.Register(
            "TimeMapDataProvider", 
            typeof<ITimeMapDataProvider>, 
            typeof<TimeMapControl>, 
            PropertyMetadata(
                EmptyTimeMapDataProvider(),
                PropertyChangedCallback(VisualChildrenGeneratingControlBase.TriggerRedraw)
        ))
    member x.TimeMapDataProvider
        with get() = x.GetValue(TimeMapDataProviderProperty) :?> ITimeMapDataProvider
        and  set(v : ITimeMapDataProvider) = x.SetValue(TimeMapDataProviderProperty, v)
    member x.ShowDetail = showDetailEvent.Publish
    override control.Redraw() = redraw()
    override control.CalculateSize = sz
    override x.OnVisualMouseEnter visual =
        let vO = blockDetailMap |> List.tryFind (fun (v,b) -> v = visual)
        if Option.isSome vO then
            let detail = snd (Option.get vO)
            showDetailEvent.Trigger(detail)
        ()
        
