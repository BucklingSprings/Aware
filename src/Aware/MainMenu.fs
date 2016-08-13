namespace BucklingSprings.Aware.Windows

open System
open System.Windows
open System.Windows.Controls

type MainMenu() as uc =
    inherit UserControl()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/MainMenu.xaml", UriKind.Relative)) :?> UserControl

    do
        uc.Content <- content
