namespace BucklingSprings.Aware

open System
open System.IO
open System.Windows
open System.Windows.Threading
open System.Windows.Controls
open System.Windows.Media.Imaging
open System.ComponentModel
open System.Collections.ObjectModel

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Controls.Labels
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.ActivitySamples
open BucklingSprings.Aware.Common.UserConfiguration

type ReplaySampleViewModel(title : string, subTitle : string, screenPath : string, time : DateTimeOffset, wordsSoFar : int, className : string, classColor : Media.Brush) =
    member x.Title = title
    member x.SubTitle = subTitle
    member x.ScreenImage = if String.IsNullOrEmpty screenPath then null else BitmapImage(Uri(screenPath))
    member x.Time = Humanize.time time
    member x.WordsSoFar = wordsSoFar
    member x.ClassName = className
    member x.ClassColor = classColor


module ReplaySampleStore =

    let nullSample (dt : DateTimeOffset) = ReplaySampleViewModel("--", "na", null, dt, 0, "--", BucklingSprings.Aware.Common.Themes.Theme.awareBrush)

    let classDetails (cfg : UserGlobalConfiguration) (s : Models.ActivitySample) =
        let classClassifier = cfg.classification.classifiers |> Seq.find (fun (c : Models.Classifier) -> c.classifierName = "Categories")
        let assignment = s.classes |> Seq.tryFind (fun sca -> sca.classifierIdentifier = classClassifier.id)
        if Option.isNone assignment then
            "Other", BucklingSprings.Aware.Common.Themes.Theme.awareBrush:> Media.Brush
        else
            let sca = Option.get assignment
            let id = Measurement.ClassIdentifier sca.assignedClass.id
            cfg.classification.classNames id, (cfg.classification.colorMap id).back
        

    let sampleForDay (dt : DateTimeOffset) (screenImagePath : string) (cfg : UserGlobalConfiguration) : seq<ReplaySampleViewModel> =
        let samples =  ActivitySamplesStore.samplesForDay dt

        let closestScreenImagePath (dt : DateTimeOffset) (m : int) =
            let sampleTime = System.TimeSpan.FromMilliseconds(float SystemConfiguration.sampleTimeInMilliseconds)
            let min = let start = dt.AddMinutes(float m) in start.Add(sampleTime).MinsSinceStartOfDay
            let entryName = BinaryStore.entryName DeepSampleType.ScreenCapture min
            let fullPath = Path.Combine(screenImagePath, entryName)
            if File.Exists fullPath then
                fullPath
            else
                null

        let fromSample (s : Models.ActivitySample) (wordsSoFar : ref<int>) : seq<ReplaySampleViewModel> =
            if s.inputActivity.IsNotIdle then
                wordsSoFar := !wordsSoFar + TypedActivitySamples.keyStrokesToWords s.inputActivity.keyboardActivity
                // only expand sample if it is a non idle sample
                let startDt = s.sampleStartTimeAndDate
                let endDt = s.sampleEndTimeAndDate
                let mins = let ts = endDt - startDt in ts.TotalMinutes |> (round >> int)
                seq { for i in 0..mins do
                                            let name, color = classDetails cfg s
                                            let sample = ReplaySampleViewModel(
                                                                s.activityWindowDetail.windowText, 
                                                                s.activityWindowDetail.processInformation.processName, 
                                                                closestScreenImagePath s.sampleStartTimeAndDate i, 
                                                                s.sampleStartTimeAndDate, 
                                                                !wordsSoFar, 
                                                                name, 
                                                                color)
                                            yield sample
                }
            else
                Seq.singleton (ReplaySampleViewModel(s.activityWindowDetail.windowText, 
                                                    s.activityWindowDetail.processInformation.processName, 
                                                    closestScreenImagePath s.sampleStartTimeAndDate 0, 
                                                    s.sampleStartTimeAndDate, 
                                                    s.inputActivity.keyboardActivity, 
                                                    "Idle", 
                                                    BucklingSprings.Aware.Common.Themes.Theme.awareBrush))

        if List.isEmpty samples then
            Seq.singleton (nullSample dt)
        else
            let byStartTime = samples
                                |> List.filter (fun s -> s.inputActivity.IsNotIdle)
                                |> List.sortBy (fun s -> s.sampleStartTimeAndDate)
            let wordsSoFar = ref 0
            seq { for s in byStartTime do
                    yield! (fromSample s wordsSoFar)
            }
        



type ReplayViewModel(dt : DateTimeOffset, instanceId : Guid, cfg : UserGlobalConfiguration) as vm  =
    let tempFolder = Path.Combine(Path.GetTempPath(), sprintf "AwareReplay_%s" (instanceId.ToString()))
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let samples = ObservableCollection<ReplaySampleViewModel>()
    let mutable currentSample = ReplaySampleStore.nullSample dt
    let mutable currentIndex = 0
    let mutable incrementBy = 1
    let switchToSample (newIndex : int) =
            if newIndex < 0 then
                currentIndex <- 0
            elif currentIndex >= samples.Count then
                currentIndex <- samples.Count-1
            else
                currentIndex <- newIndex
            currentSample <- samples.Item(currentIndex)
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("CurrentSample"))  
    let toggle dir () =
        if incrementBy = dir then
            incrementBy <- 0
        else
            incrementBy <- dir
            vm.SwitchToNext ()
        ()
    do
        
        if Directory.Exists tempFolder then
            Directory.Delete(tempFolder,true)
        Directory.CreateDirectory tempFolder |> ignore
        DeepInspection.exportSamplesForDayTo dt tempFolder

        ReplaySampleStore.sampleForDay dt tempFolder cfg
            |> Seq.iter (fun x -> samples.Add(x))

    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    member this.CurrentSample = currentSample
    member x.MinIndex = 0
    member x.MaxIndex = samples.Count - 1
    member x.ToggleBackward = AlwaysExecutableCommand(toggle -1)
    member x.ToggleForward = AlwaysExecutableCommand(toggle 1)
    member x.Date = dt
    member x.Index
        with get () = currentIndex
        and set (newIndex : int) = 
            switchToSample newIndex
            incrementBy <- 0
            
    member x.SwitchToNext () =
        if incrementBy = 0 then
            ()
        else
            let index = currentIndex + incrementBy
            let newIndex = if index > x.MaxIndex then
                                x.MinIndex
                            elif index < x.MinIndex then
                                x.MaxIndex
                            else
                                index

            switchToSample newIndex
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("Index")) 
            

    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

type DesignTimeReplayViewModel() =
    member x.CurrentSample = ReplaySampleStore.nullSample (DateTimeOffset.Now)
    member x.MinIndex = 0
    member x.MaxIndex = 100
    member x.Index 
        with get() = 50
        and set(newIndex: int) = ()

type ReplayWindow(vm : ReplayViewModel) as w =
    inherit Window()
    let switchToNext _ _ =
        vm.SwitchToNext ()
    let timer = DispatcherTimer(TimeSpan.FromMilliseconds(200.0), DispatcherPriority.Normal, EventHandler(switchToNext), w.Dispatcher) 
    do
        let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ReplayWindow.xaml", UriKind.Relative)) :?> UserControl
        w.Title <- "Aware Replay"
        w.Icon <- BitmapImage(Uri("pack://application:,,,/BucklingSprings.Aware;component/Aware.ico", UriKind.Absolute))
        w.Content <- content
        let replayDateLabel = content.FindName("ReplayDateLabel") :?> CalendarStyleDateLabel
        replayDateLabel.Date <- vm.Date
        w.DataContext <- vm


    
module Replay =

    let show (day : DateTimeOffset) (cfg : UserGlobalConfiguration) =
        let window = ReplayWindow(ReplayViewModel(day, Guid.NewGuid(), cfg))
        window.Show()

