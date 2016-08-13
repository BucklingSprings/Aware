namespace BucklingSprings.Aware.Controls.Labels

open BucklingSprings.Aware.Core.Utils

open System
open System.Windows
open System.Windows.Controls

type DatePartsViewModel(dt : System.DateTimeOffset) =
    member x.Month =  Humanize.monthAbbrev dt
    member x.Day = let d = dt.Day in d.ToString()
    member x.Year = let d = dt.Year in d.ToString()


type CalendarStyleDateLabel() as ctl =
    inherit UserControl()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware.Controls;component/CalendarStyleDateLabel.xaml", UriKind.RelativeOrAbsolute)) :?> UserControl

    let redraw () =
        content.DataContext <- DatePartsViewModel(ctl.Date)

    do
        ctl.Content <- content
        ctl.Loaded.Add(fun e -> redraw())

    static let DateProperty =
        DependencyProperty.Register(
                                     "CalendarStyleDateLabel",
                                     typeof<DateTimeOffset>,
                                     typeof<CalendarStyleDateLabel>,
                                     new PropertyMetadata(
                                        DateTimeOffset.Now, 
                                        new PropertyChangedCallback(CalendarStyleDateLabel.RedrawOnDatePropertyChanged)))

    
    member this.Redraw () = redraw()
    static member RedrawOnDatePropertyChanged (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? CalendarStyleDateLabel as c -> c.Redraw()
        | _ -> ()
    member x.Date
        with get() = x.GetValue(DateProperty) :?> DateTimeOffset
        and  set(v : DateTimeOffset) = x.SetValue(DateProperty, v)