namespace BucklingSprings.Aware.Controls.Labels

open System
open System.Windows
open System.Windows.Controls

type BallonLabelViewModel(s : string) =
    member x.Text = s

type BaloonLabel() as uc =
    inherit UserControl()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware.Controls;component/BaloonLabel.xaml", UriKind.RelativeOrAbsolute)) :?> UserControl

    let redraw () =
        content.DataContext <- BallonLabelViewModel(uc.Text)

    do
        uc.Content <- content

    static let TextProperty =
        DependencyProperty.Register(
                                     "TextProperty",
                                     typeof<string>,
                                     typeof<BaloonLabel>,
                                     new PropertyMetadata(
                                        System.String.Empty, 
                                        new PropertyChangedCallback(BaloonLabel.RedrawOnTextPropertyChanged)))

    
    member this.Redraw () = redraw()
    static member RedrawOnTextPropertyChanged (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? BaloonLabel as c -> c.Redraw()
        | _ -> ()
    member x.Text
        with get() = x.GetValue(TextProperty) :?> string
        and  set(v : string) = x.SetValue(TextProperty, v)