namespace BucklingSprings.Aware.SampleWatch

open System
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Controls
open System.Windows.Threading

open BucklingSprings.Aware.Core.ActivitySamples
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.Utils

type SampleViewModel(p : string, t : string, w : int, s : DateTimeOffset, e: DateTimeOffset) =
    member x.Program = p
    member x.Title = t
    member x.Words = TypedActivitySamples.keyStrokesToWords w
    member x.Minutes = e.Subtract(s).TotalMinutes
    member x.FromTo = String.Format("{0:yyyy/MM/dd hh:mm:ss} - {1:yyyy/MM/dd hh:mm:ss}", s, e)

type SampleWatchMainWindowViewModel(samples : ObservableCollection<SampleViewModel>) =
    member x.LastFewSamples = samples;

type DesignTimeSampleWatchMainWindowViewModel() =
    let samples = ObservableCollection<SampleViewModel>()
    do
        samples.Add(SampleViewModel("Windows Explorer", "C:\SomeFolder", 23, DateTimeOffset.Now.AddMinutes(-10.0), DateTimeOffset.Now.AddMinutes(-1.2)))
        samples.Add(SampleViewModel("Google Chrome", " Example.com ", 7, DateTimeOffset.Now.AddMinutes(-11.0), DateTimeOffset.Now.AddMinutes(-10.0)))
    member x.LastFewSamples = samples;

type SampleWatchMainWindow() as w =
    inherit Window()

    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware.SampleWatch;component/SampleWatchMainWindow.xaml", UriKind.Relative)) :?> UserControl
    let refreshTimer = DispatcherTimer(DispatcherPriority.ContextIdle)
    let showSamples () = 
        let actSamples = ActivitySamplesStore.lastNSamples 20
        let samples = ObservableCollection<SampleViewModel>()
        let toVm (s : ActivitySample) : SampleViewModel =
            SampleViewModel(s.activityWindowDetail.processInformation.processName, s.activityWindowDetail.windowText, s.inputActivity.keyboardActivity, s.sampleStartTimeAndDate, s.sampleEndTimeAndDate)
        actSamples |> List.iter (toVm >> samples.Add)
        content.DataContext <- SampleWatchMainWindowViewModel(samples)

    do
        w.Title <- "Aware - Sample Watcher"
        w.Width <- 600.0
        w.Height <- 500.0
        w.Topmost <- true
        w.Content <- content
        refreshTimer.Interval <- System.TimeSpan.FromMinutes(1.0)
        showSamples()
        refreshTimer.Tick.Add(fun _ -> showSamples())
        w.Closing.Add(fun _ -> refreshTimer.IsEnabled <- false)



