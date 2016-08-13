#nowarn "52"

namespace BucklingSprings.Aware.Windows


open BucklingSprings.Aware
open BucklingSprings.Aware.Threading
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.ViewModels
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Controls.Dates
open BucklingSprings.Aware.Controls.Composite
open BucklingSprings.Aware.Controls.Labels

open System
open System.Windows
open System.Windows.Input
open System.Windows.Controls
open System.Windows.Threading
open System.Windows.Media
open System.Windows.Media.Imaging

type DashboardWindow() as window =
    inherit Window()
    let configurationService = ConfigurationService() :> IConfigurationService
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/DashboardWindow.xaml", UriKind.Relative)) :?> UserControl
    let legend = content.FindName("Legend") :?> ClassificationClassFilterAndLegendControl
    let mainFrame = content.FindName("MainFrame") :?> Frame
    let wds = WorkingDataService(configurationService)
    let refreshTimer = DispatcherTimer(DispatcherPriority.ContextIdle)
    let flatOverlay = content.FindName("FlatOverlay") :?> Image
    let flashMessageDisplay = content.FindName("FlashMessageDisplay") :?> FlashMessageControl
    
   
    let dateRangeSelection = content.FindName("DateRangeSelector") :?> DateRangeSelectorControl
    let installInfo : Updates.InstallInformation = 
        {
            Updates.InstallInformation.Product = "Aware";
            Updates.InstallInformation.SerialNumber = "";
            Updates.InstallInformation.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
        }
    let showUpdate (up : Updates.UpgradeInformation) =
        window.Dispatcher.Invoke(fun e ->
            let updatesWindow = UpdateWindow(up, window)
            updatesWindow.Show() |> ignore)



   

    do
        window.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        window.WindowState <- WindowState.Maximized
//        window.WindowState <- WindowState.Normal
//        window.Width <- 852.0 * 2.0
//        window.Height <- 335.0 * 3.0
        
        window.Title <- "Aware"
        window.Icon <- BitmapImage(Uri("pack://application:,,,/BucklingSprings.Aware;component/Aware.ico", UriKind.Absolute))
        refreshTimer.Interval <- System.TimeSpan.FromMinutes(1.0)
        refreshTimer.Tick.Add(fun _ -> Async.Start(wds.Refresh()))
        window.Content <- content

        Async.Start(Updates.updateAvailableAsync installInfo showUpdate)
        Async.Start(HouseKeeping.startUpAsync())

        
        
        configurationService.ConfigurationReloaded.Add(fun e -> legend.Redraw())
        

        window.PreviewKeyUp.Add(fun e ->
            if e.Key = Key.F2 then
                if flatOverlay.Visibility = Visibility.Hidden then
                    flatOverlay.Visibility <- Visibility.Visible
                else
                    flatOverlay.Visibility <- Visibility.Hidden)


        window.SizeChanged.Add(fun e ->
            if e.NewSize.Height < 900.0 then
                let scale = e.NewSize.Height / 900.0
                content.LayoutTransform <- ScaleTransform(scale, scale)
            else
                content.LayoutTransform <- null
            )
             

        window.Loaded.Add(fun e -> 
                UIContext.initialize()
                let messageService = { new IMessageService with
                                            member x.Display s =
                                                window.Dispatcher.Invoke(fun _ -> 
                                                    flashMessageDisplay.FlashMessage <- s
                                                    flashMessageDisplay.ReDraw())
                                        }
                let navigator (o : obj) = (mainFrame.Navigate(o) |> ignore)
                let vm = DashboardViewModel(configurationService, wds, navigator, messageService)
                window.DataContext <- vm
                refreshTimer.Start())

 

        dateRangeSelection.SelectionChanged.Add(fun e ->
                                                    let filter = DataDateRangeFilter.FilterDataTo(e.StartDate, e.EndDate)
                                                    Async.Start(configurationService.UpdateDateRangeAsync filter)
                                                )


