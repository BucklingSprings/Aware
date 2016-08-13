namespace BucklingSprings.Aware.Tiles

open System
open System.Windows
open System.Windows.Media
open System.Windows.Controls

open BucklingSprings.Aware.Common.Themes

type TilesHoursMinutesViewModel(h : int, m : int) =
    member x.Hours = h
    member x.Minutes = m

type TilesHoursMinutes() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesHoursMinutes.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.Minutes >= 0 then
            let ts = System.TimeSpan.FromMinutes(float uc.Minutes)
            let hrs = ts.TotalHours |> floor |> int
            container.DataContext <- TilesHoursMinutesViewModel(hrs,ts.Minutes)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    
    
    static let MinutesProperty =
        DependencyProperty.Register(
                                     "Minutes",
                                     typeof<int>,
                                     typeof<TilesHoursMinutes>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesHoursMinutes.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesHoursMinutes as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.Minutes
        with get() = x.GetValue(MinutesProperty) :?> int
        and  set(v : int) = x.SetValue(MinutesProperty, v)


type TilesWordsViewModel(w : int) =
    member x.Words = w

type TilesWords() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWords.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.Words >= 0 then
            container.DataContext <- TilesWordsViewModel(uc.Words)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    
    
    static let WordsProperty =
        DependencyProperty.Register(
                                     "Words",
                                     typeof<int>,
                                     typeof<TilesWords>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWords.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWords as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.Words
        with get() = x.GetValue(WordsProperty) :?> int
        and  set(v : int) = x.SetValue(WordsProperty, v)


type TilesTimeOfDayViewModel(t : int) =
    member x.Time = t

type TilesTimeOfDayBase(xaml : string) as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri(sprintf "/BucklingSprings.Aware;component/%s.xaml" xaml, UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.Time >= 0 then
            container.DataContext <- TilesTimeOfDayViewModel(uc.Time)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    
    
    static let TimeProperty =
        DependencyProperty.Register(
                                     "Time",
                                     typeof<int>,
                                     typeof<TilesTimeOfDayBase>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesTimeOfDayBase.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesTimeOfDayBase as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.Time
        with get() = x.GetValue(TimeProperty) :?> int
        and  set(v : int) = x.SetValue(TimeProperty, v)

type TilesTimeOfDayStart()  =
    inherit TilesTimeOfDayBase("TilesTimeOfDayStart")

type TilesTimeOfDayEnd()  =
    inherit TilesTimeOfDayBase("TilesTimeOfDayEnd")

type TilesLegendViewModel(b : Brush, t : string) =
    member x.LegendBrush = b
    member x.LegendText = t

type TilesLegend() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesLegend.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        container.DataContext <- TilesLegendViewModel(uc.LegendBrush, uc.LegendText)
    do
        uc.Content <- content
    
    static let LegendBrushProperty =
        DependencyProperty.Register(
                                     "LegendBrush",
                                     typeof<Brush>,
                                     typeof<TilesLegend>,
                                     new PropertyMetadata(Theme.awareBrush, new PropertyChangedCallback(TilesLegend.TriggerRedraw)))

    static let LegendTextProperty =
        DependencyProperty.Register(
                                     "LegendText",
                                     typeof<string>,
                                     typeof<TilesLegend>,
                                     new PropertyMetadata("--", new PropertyChangedCallback(TilesLegend.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesLegend as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.LegendBrush
        with get() = x.GetValue(LegendBrushProperty) :?> Brush
        and  set(v : Brush) = x.SetValue(LegendBrushProperty, v)

    member x.LegendText
        with get() = x.GetValue(LegendTextProperty) :?> string
        and  set(v : string) = x.SetValue(LegendTextProperty, v)


type TilesVerticalWithSubtextLegendViewModel(b : Brush, t : string, s : string) =
    member x.LegendBrush = b
    member x.LegendText = t
    member x.LegendSubText = s

type TilesVerticalWithSubtextLegend() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesVerticalWithSubtextLegend.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        container.DataContext <- TilesVerticalWithSubtextLegendViewModel(uc.LegendBrush, uc.LegendText, uc.LegendSubText)
    do
        uc.Content <- content
    
    static let LegendBrushProperty =
        DependencyProperty.Register(
                                     "LegendBrush",
                                     typeof<Brush>,
                                     typeof<TilesVerticalWithSubtextLegend>,
                                     new PropertyMetadata(Theme.awareBrush, new PropertyChangedCallback(TilesVerticalWithSubtextLegend.TriggerRedraw)))

    static let LegendTextProperty =
        DependencyProperty.Register(
                                     "LegendText",
                                     typeof<string>,
                                     typeof<TilesVerticalWithSubtextLegend>,
                                     new PropertyMetadata("--", new PropertyChangedCallback(TilesVerticalWithSubtextLegend.TriggerRedraw)))

    static let LegendSubTextProperty =
        DependencyProperty.Register(
                                    "LegendSubText",
                                    typeof<string>,
                                    typeof<TilesVerticalWithSubtextLegend>,
                                    new PropertyMetadata("--", new PropertyChangedCallback(TilesVerticalWithSubtextLegend.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesVerticalWithSubtextLegend as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.LegendBrush
        with get() = x.GetValue(LegendBrushProperty) :?> Brush
        and  set(v : Brush) = x.SetValue(LegendBrushProperty, v)

    member x.LegendText
        with get() = x.GetValue(LegendTextProperty) :?> string
        and  set(v : string) = x.SetValue(LegendTextProperty, v)

    member x.LegendSubText
        with get() = x.GetValue(LegendSubTextProperty) :?> string
        and  set(v : string) = x.SetValue(LegendSubTextProperty, v)

type TilesWordRangeViewModel(l : int, h : int) =
    member x.WordsLow = l
    member x.WordsHigh = h

type TilesWordRange() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWordRange.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.WordsLow >= 0 && uc.WordsHigh >= 0 then
            container.DataContext <- TilesWordRangeViewModel(uc.WordsLow, uc.WordsHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let WordsLowProperty =
        DependencyProperty.Register(
                                     "WordsLow",
                                     typeof<int>,
                                     typeof<TilesWordRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordRange.TriggerRedraw)))
    static let WordsHighProperty =
        DependencyProperty.Register(
                                     "WordsHigh",
                                     typeof<int>,
                                     typeof<TilesWordRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordRange.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWordRange as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.WordsLow
        with get() = x.GetValue(WordsLowProperty) :?> int
        and  set(v : int) = x.SetValue(WordsLowProperty, v)
    
    member x.WordsHigh
        with get() = x.GetValue(WordsHighProperty) :?> int
        and  set(v : int) = x.SetValue(WordsHighProperty, v)


type TilesMinuteRangeViewModel(l : int, h : int) =
    member x.MinutesLow = l
    member x.MinutesHigh = h

type TilesMinuteRange() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesMinuteRange.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.MinutesLow >= 0 && uc.MinutesHigh >= 0 then
            container.DataContext <- TilesMinuteRangeViewModel(uc.MinutesLow, uc.MinutesHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let MinutesLowProperty =
        DependencyProperty.Register(
                                     "MinutesLow",
                                     typeof<int>,
                                     typeof<TilesMinuteRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesMinuteRange.TriggerRedraw)))
    static let MinutesHighProperty =
        DependencyProperty.Register(
                                     "MinutesHigh",
                                     typeof<int>,
                                     typeof<TilesMinuteRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesMinuteRange.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesMinuteRange as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.MinutesLow
        with get() = x.GetValue(MinutesLowProperty) :?> int
        and  set(v : int) = x.SetValue(MinutesLowProperty, v)
    
    member x.MinutesHigh
        with get() = x.GetValue(MinutesHighProperty) :?> int
        and  set(v : int) = x.SetValue(MinutesHighProperty, v)


type TilesDayHourViewModel(h : int, d : System.DayOfWeek) =
    member x.Hour = h
    member x.Day = d

type TilesDayHour() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesDayHour.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.Hour >= 0 then
            container.DataContext <- TilesDayHourViewModel(uc.Hour, uc.Day)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let HourProperty =
        DependencyProperty.Register(
                                     "Hour",
                                     typeof<int>,
                                     typeof<TilesDayHour>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesDayHour.TriggerRedraw)))
    static let DayProperty =
        DependencyProperty.Register(
                                     "Day",
                                     typeof<System.DayOfWeek>,
                                     typeof<TilesDayHour>,
                                     new PropertyMetadata(System.DayOfWeek.Sunday, new PropertyChangedCallback(TilesDayHour.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesDayHour as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.Hour
        with get() = x.GetValue(HourProperty) :?> int
        and  set(v : int) = x.SetValue(HourProperty, v)
    
    member x.Day
        with get() = x.GetValue(DayProperty) :?> System.DayOfWeek
        and  set(v : System.DayOfWeek) = x.SetValue(DayProperty, v)


type TilesWordsPerMinuteViewModel(w : int) =
    member x.WordsPerMinute = w

type TilesWordsPerMinute() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWordsPerMinute.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.WordsPerMinute > 0 then
            container.DataContext <- TilesWordsPerMinuteViewModel(uc.WordsPerMinute)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    
    
    static let WordsPerMinuteProperty =
        DependencyProperty.Register(
                                     "WordsPerMinute",
                                     typeof<int>,
                                     typeof<TilesWordsPerMinute>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordsPerMinute.TriggerRedraw)))
    
    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWordsPerMinute as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    member x.WordsPerMinute
        with get() = x.GetValue(WordsPerMinuteProperty) :?> int
        and  set(v : int) = x.SetValue(WordsPerMinuteProperty, v)


type TilesWordsPerMinuteRangeViewModel(l : int, h : int) =
    member x.WordsPerMinuteLow = l
    member x.WordsPerMinuteHigh = h

type TilesWordsPerMinuteRange() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWordsPerMinuteRange.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.WordsPerMinuteLow >= 0 && uc.WordsPerMinuteHigh >= 0 then
            container.DataContext <- TilesWordsPerMinuteRangeViewModel(uc.WordsPerMinuteLow, uc.WordsPerMinuteHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let WordsPerMinuteLowProperty =
        DependencyProperty.Register(
                                     "WordsPerMinuteLow",
                                     typeof<int>,
                                     typeof<TilesWordsPerMinuteRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordsPerMinuteRange.TriggerRedraw)))
    static let WordsPerMinuteHighProperty =
        DependencyProperty.Register(
                                     "WordsPerMinuteHigh",
                                     typeof<int>,
                                     typeof<TilesWordsPerMinuteRange>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordsPerMinuteRange.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWordsPerMinuteRange as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.WordsPerMinuteLow
        with get() = x.GetValue(WordsPerMinuteLowProperty) :?> int
        and  set(v : int) = x.SetValue(WordsPerMinuteLowProperty, v)
    
    member x.WordsPerMinuteHigh
        with get() = x.GetValue(WordsPerMinuteHighProperty) :?> int
        and  set(v : int) = x.SetValue(WordsPerMinuteHighProperty, v)



type TilesHourMinuteRangeSlimViewModel(l : int, h : int) =
    let low = TimeSpan.FromMinutes(float l)
    let high = TimeSpan.FromMinutes(float h)
    member x.HoursLow = low.TotalHours |> floor |> int
    member x.MinutesLow = low.Minutes
    member x.HoursHigh = high.TotalHours |> floor |> int
    member x.MinutesHigh = high.Minutes

type TilesHourMinuteRangeSlim() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesHourMinuteRangeSlim.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.MinutesLow >= 0 && uc.MinutesHigh >= 0 then
            container.DataContext <- TilesHourMinuteRangeSlimViewModel(uc.MinutesLow, uc.MinutesHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let MinutesLowProperty =
        DependencyProperty.Register(
                                     "MinutesLow",
                                     typeof<int>,
                                     typeof<TilesHourMinuteRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesHourMinuteRangeSlim.TriggerRedraw)))
    static let MinutesHighProperty =
        DependencyProperty.Register(
                                     "MinutesHigh",
                                     typeof<int>,
                                     typeof<TilesHourMinuteRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesHourMinuteRangeSlim.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesHourMinuteRangeSlim as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.MinutesLow
        with get() = x.GetValue(MinutesLowProperty) :?> int
        and  set(v : int) = x.SetValue(MinutesLowProperty, v)
    
    member x.MinutesHigh
        with get() = x.GetValue(MinutesHighProperty) :?> int
        and  set(v : int) = x.SetValue(MinutesHighProperty, v)

type TilesWordRangeSlimViewModel(l : int, h : int) =
    member x.WordsLow = l
    member x.WordsHigh = h

type TilesWordRangeSlim() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWordRangeSlim.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.WordsLow >= 0 && uc.WordsHigh >= 0 then
            container.DataContext <- TilesWordRangeSlimViewModel(uc.WordsLow, uc.WordsHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let WordsLowProperty =
        DependencyProperty.Register(
                                     "WordsLow",
                                     typeof<int>,
                                     typeof<TilesWordRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordRangeSlim.TriggerRedraw)))
    static let WordsHighProperty =
        DependencyProperty.Register(
                                     "WordsHigh",
                                     typeof<int>,
                                     typeof<TilesWordRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordRangeSlim.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWordRangeSlim as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.WordsLow
        with get() = x.GetValue(WordsLowProperty) :?> int
        and  set(v : int) = x.SetValue(WordsLowProperty, v)
    
    member x.WordsHigh
        with get() = x.GetValue(WordsHighProperty) :?> int
        and  set(v : int) = x.SetValue(WordsHighProperty, v)


type TilesWordsPerMinuteRangeSlimViewModel(l : int, h : int) =
    member x.WordsPerMinutesLow = l
    member x.WordsPerMinutesHigh = h

type TilesWordsPerMinuteRangeSlim() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesWordsPerMinuteRangeSlim.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.WordsPerMinutesLow >= 0 && uc.WordsPerMinutesHigh >= 0 then
            container.DataContext <- TilesWordsPerMinuteRangeSlimViewModel(uc.WordsPerMinutesLow, uc.WordsPerMinutesHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let WordsPerMinutesLowProperty =
        DependencyProperty.Register(
                                     "WordsPerMinutesLow",
                                     typeof<int>,
                                     typeof<TilesWordsPerMinuteRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordsPerMinuteRangeSlim.TriggerRedraw)))
    static let WordsPerMinutesHighProperty =
        DependencyProperty.Register(
                                     "WordsPerMinutesHigh",
                                     typeof<int>,
                                     typeof<TilesWordsPerMinuteRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesWordsPerMinuteRangeSlim.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesWordsPerMinuteRangeSlim as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.WordsPerMinutesLow
        with get() = x.GetValue(WordsPerMinutesLowProperty) :?> int
        and  set(v : int) = x.SetValue(WordsPerMinutesLowProperty, v)
    
    member x.WordsPerMinutesHigh
        with get() = x.GetValue(WordsPerMinutesHighProperty) :?> int
        and  set(v : int) = x.SetValue(WordsPerMinutesHighProperty, v)

type TilesTimeOfDayRangeSlimViewModel(l : int, h : int) =
    member x.TimeLow = l
    member x.TimeHigh = h

type TilesTimeOfDayRangeRangeSlim() as uc =
    inherit UserControl()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/TilesTimeOfDayRangeRangeSlim.xaml", UriKind.Relative)) :?> UserControl
    let container = content.FindName("TileContainer") :?> Grid
    let redraw () =
        if uc.TimeLow >= 0 && uc.TimeHigh >= 0 then
            container.DataContext <- TilesTimeOfDayRangeSlimViewModel(uc.TimeLow, uc.TimeHigh)
        else
            container.DataContext <- null
    do
        uc.Content <- content

    static let TimeLowProperty =
        DependencyProperty.Register(
                                     "TimeLow",
                                     typeof<int>,
                                     typeof<TilesTimeOfDayRangeRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesTimeOfDayRangeRangeSlim.TriggerRedraw)))
    static let TimeHighProperty =
        DependencyProperty.Register(
                                     "TimeHigh",
                                     typeof<int>,
                                     typeof<TilesTimeOfDayRangeRangeSlim>,
                                     new PropertyMetadata(-1, new PropertyChangedCallback(TilesTimeOfDayRangeRangeSlim.TriggerRedraw)))

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? TilesTimeOfDayRangeRangeSlim as t -> t.Redraw()
        | _ -> ()
    
    member x.Redraw () = redraw()
    
    member x.TimeLow
        with get() = x.GetValue(TimeLowProperty) :?> int
        and  set(v : int) = x.SetValue(TimeLowProperty, v)
    
    member x.TimeHigh
        with get() = x.GetValue(TimeHighProperty) :?> int
        and  set(v : int) = x.SetValue(TimeHighProperty, v)