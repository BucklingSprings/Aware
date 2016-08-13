#nowarn "52"

namespace BucklingSprings.Aware.Controls.Dates

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Media

open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Controls.Labels

type DateRangeSelectorSelectionChanged(startDt : System.DateTimeOffset, endDt : System.DateTimeOffset) =
    inherit System.EventArgs()
    member val StartDate = startDt with get
    member val EndDate = endDt with get

type DateRangeSelectorControl() as ctl =
    inherit UserControl()

    let selectionChanged = new Event<DateRangeSelectorSelectionChanged>()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware.Controls;component/DateRangeSelectorControl.xaml", UriKind.RelativeOrAbsolute)) :?> UserControl
    let track = content.FindName("Track") :?> Border
    let leftThumb = content.FindName("LeftThumb") :?> Thumb
    let windowThumb = content.FindName("WindowThumb") :?> Thumb
    let rightThumb = content.FindName("RightThumb") :?> Thumb
    let startDateLabel = content.FindName("StartDateLabel") :?> CalendarStyleDateLabel
    let endDateLabel = content.FindName("EndDateLabel") :?> CalendarStyleDateLabel
    let currentStartDateLabel = content.FindName("CurrentStartDateLabel") :?> BaloonLabel
    let currentEndDateLabel = content.FindName("CurrentEndDateLabel") :?> BaloonLabel
    let mutable minimumWindowSize = 0.0
    let mutable dragging = false
    let mutable currentStartDate = System.DateTimeOffset.Now.StartOfDay
    let mutable currentEndDate = System.DateTimeOffset.Now.StartOfDay

    let differenceInDays (s : System.DateTimeOffset) (e : System.DateTimeOffset) : float  = ceil (e.Subtract(s).TotalDays)

    let left (o : DependencyObject) = 
        let m = o.GetValue(FrameworkElement.MarginProperty) :?> Thickness
        let l = m.Left
        if System.Double.IsNaN l then 0.0 else l

    let setLeft (o : DependencyObject) (v : float) = 
        let m = o.GetValue(FrameworkElement.MarginProperty) :?> Thickness
        let t = Thickness(v, m.Top, m.Right, m.Bottom)
        o.SetValue(FrameworkElement.MarginProperty, t)


    let setLeftConstrained (o : DependencyObject) (left : float) minLeft maxLeft =
        let l = if left > maxLeft then
                    maxLeft
                elif left < minLeft then
                    minLeft
                else
                    left
        setLeft o l
    
    let allowedThumbLeftLeft possibleNewLeft =
        let startPos =  possibleNewLeft
        let width = max ((left rightThumb) - startPos) 0.0
        let allowedWidth  =  (ceil (width / minimumWindowSize)) * minimumWindowSize
        let allowedNewLeft = (left rightThumb) - allowedWidth
        allowedNewLeft

    let allowedThumbRightLeft possibleNewLeft =
        let startPos =  left leftThumb
        let width = max (possibleNewLeft - startPos) 0.0
        let allowedWidth  =  (ceil (width / minimumWindowSize)) * minimumWindowSize
        let allowedNewLeft = (left leftThumb) + allowedWidth
        allowedNewLeft

    let allowedWindowThumbLeft possibleNewLeft =
        let allowedLeft  =  (ceil (possibleNewLeft / minimumWindowSize)) * minimumWindowSize
        allowedLeft

    let resizeWindowThumb () =
        let startPos =  left leftThumb
        let width = max ((left rightThumb) - startPos) 0.0
        windowThumb.Width <- width
        setLeft windowThumb startPos

    let repositionLeftRightThumbs () =
        let windowLeft = left windowThumb
        setLeft leftThumb windowLeft
        setLeft currentStartDateLabel windowLeft
        setLeft rightThumb (windowLeft + windowThumb.Width)
        setLeft currentEndDateLabel (windowLeft + windowThumb.Width)

    let setSelection (final) =
        let l = (left windowThumb) / minimumWindowSize
        let w = (windowThumb.Width) / minimumWindowSize
        let rangeStart : System.DateTimeOffset = ctl.DateRangeStartDate
        currentStartDate <- rangeStart.AddDays(l)
        currentStartDateLabel.Text <- sprintf "%s%d" (Humanize.monthAbbrev currentStartDate) (currentStartDate.Day)
        currentEndDate <- currentStartDate.AddDays(w)
        currentEndDateLabel.Text <- sprintf "%s%d" (Humanize.monthAbbrev currentEndDate) (currentEndDate.Day)
        if final then
            selectionChanged.Trigger(DateRangeSelectorSelectionChanged(currentStartDate.StartOfDay, currentEndDate.StartOfDay))

        
    let leftFromDays pixelPerDays (date : System.DateTimeOffset) =
        date.Subtract(ctl.DateRangeStartDate).TotalDays * pixelPerDays

    let redraw () =
        startDateLabel.Date <- ctl.DateRangeStartDate
        endDateLabel.Date <- ctl.DateRangeEndDate
        let days = differenceInDays ctl.DateRangeStartDate ctl.DateRangeEndDate
        let pixelPerDays = track.ActualWidth / (differenceInDays ctl.DateRangeStartDate ctl.DateRangeEndDate)
        minimumWindowSize <- pixelPerDays
        
        setLeft leftThumb (leftFromDays pixelPerDays currentStartDate)
        setLeft currentStartDateLabel (leftFromDays pixelPerDays currentStartDate)
        setLeft windowThumb (left leftThumb)
        setLeft rightThumb  (leftFromDays pixelPerDays currentEndDate)
        setLeft currentEndDateLabel  (leftFromDays pixelPerDays currentEndDate)
        windowThumb.Width <- (currentEndDate.Subtract(currentStartDate).TotalDays * pixelPerDays)
        setSelection(false)

    do
        leftThumb.RenderTransform <- TranslateTransform(38.0 - 9.0, 0.0)
        rightThumb.RenderTransform <- TranslateTransform(38.0 - 9.0, 0.0)
        windowThumb.RenderTransform <- TranslateTransform(38.0, 0.0)
        ctl.Content <- content
        leftThumb.DragDelta.Add(fun e ->
                                    dragging <- true
                                    let maxLeft = (left rightThumb) - minimumWindowSize
                                    let newLeft = allowedThumbLeftLeft( (left leftThumb) + e.HorizontalChange)
                                    setLeftConstrained leftThumb newLeft 0.0 maxLeft
                                    setLeftConstrained currentStartDateLabel newLeft 0.0 maxLeft
                                    resizeWindowThumb()
                                    setSelection(false)
                                )

        rightThumb.DragDelta.Add(fun e ->
                                    dragging <- true
                                    let minLeft = (left leftThumb) + minimumWindowSize
                                    let maxLeft = track.ActualWidth
                                    let newLeft = allowedThumbRightLeft ((left rightThumb) + e.HorizontalChange)
                                    setLeftConstrained rightThumb newLeft minLeft maxLeft
                                    setLeftConstrained currentEndDateLabel newLeft minLeft maxLeft
                                    resizeWindowThumb()
                                    setSelection(false)
                                )

        windowThumb.DragDelta.Add(fun e ->
                                    dragging <- true
                                    let newLeft = allowedWindowThumbLeft ((left windowThumb) + e.HorizontalChange)
                                    let minLeft = 0.0
                                    let maxLeft = track.ActualWidth - windowThumb.ActualWidth
                                    setLeftConstrained windowThumb newLeft minLeft maxLeft
                                    repositionLeftRightThumbs()
                                    setSelection(false)
                                 )

        leftThumb.DragCompleted.Add(fun e -> setSelection(true))
        rightThumb.DragCompleted.Add(fun e -> setSelection(true))
        windowThumb.DragCompleted.Add(fun e -> setSelection(true))

        ctl.SizeChanged.Add(fun e -> redraw())
    

    static let DateRangeStartDateProperty =
        DependencyProperty.Register(
                                     "DateRangeStartDate",
                                     typeof<System.DateTimeOffset>,
                                     typeof<DateRangeSelectorControl>,
                                     new PropertyMetadata(
                                        System.DateTimeOffset.Now.AddDays(-7.0), 
                                        new PropertyChangedCallback(DateRangeSelectorControl.ReDrawOnDateRangeChange)))

    
    static let DateRangeEndDateProperty =
        DependencyProperty.Register(
                                     "DateRangeEndDate",
                                     typeof<System.DateTimeOffset>,
                                     typeof<DateRangeSelectorControl>,
                                     new PropertyMetadata(
                                        System.DateTimeOffset.Now, 
                                        new PropertyChangedCallback(DateRangeSelectorControl.ReDrawOnDateRangeChange)))
    
    static member ReDrawOnDateRangeChange (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? DateRangeSelectorControl as drs -> drs.ReDraw()
        | _ -> ()

    member x.DateRangeStartDate
            with get() = x.GetValue(DateRangeStartDateProperty) :?> System.DateTimeOffset
            and  set(v : System.DateTimeOffset) = x.SetValue(DateRangeStartDateProperty, v)

    member x.DateRangeEndDate
        with get() = x.GetValue(DateRangeEndDateProperty) :?> System.DateTimeOffset
        and  set(v : System.DateTimeOffset) = x.SetValue(DateRangeEndDateProperty, v)

    member x.ReDraw () = 
        currentStartDate <- x.DateRangeStartDate
        currentEndDate <- x.DateRangeEndDate
        redraw ()

    member x.SelectionChanged = selectionChanged.Publish

