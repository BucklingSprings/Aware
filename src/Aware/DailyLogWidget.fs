#nowarn "52"

namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Common.Focus
open BucklingSprings.Aware.Data

open System
open System.Windows
open System.Collections.ObjectModel

type DailyLogData(sessions, config, date) =
    member x.Sessions = sessions
    member x.Config = config
    member x.Date = date

type DailyLogEntryViewModel(fs : FocusSession, colors : AssignedBrushes, className : string) =
    let ts = fs.endTime - fs.startTime
    member x.FocussedOnDescription = className
    member x.Minutes = ts.TotalMinutes |> ceil |> int
    member x.Words = fs.words
    member x.ClassColor = colors.back
    member x.StartTime = fs.startTime
    member x.EndTime = fs.endTime


type DesignTimeDailyLogEntryViewModel() =
    member x.FocussedOnDescription = "Microsoft Visual Studio 2012"
    member x.Minutes = 230
    member x.Words = 9876
    member x.ClassColor = (List.head Theme.customColors).back
    member x.StartTime = System.DateTimeOffset.Now.AddHours(-2.0)
    member x.EndTime = System.DateTimeOffset.Now


type DesignTimeDailyLogViewModel() =
    let focusSessions = ObservableCollection<DesignTimeDailyLogEntryViewModel>()
    do
        focusSessions.Add(DesignTimeDailyLogEntryViewModel())
        focusSessions.Add(DesignTimeDailyLogEntryViewModel())
        focusSessions.Add(DesignTimeDailyLogEntryViewModel())
    member x.FocusSessions = focusSessions

type DailyLogViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<DailyLogData>(wds, true)

  

    let focusSessions = ObservableCollection<DailyLogEntryViewModel>()
    let mutable forDate : string = null
    let mutable replayDate : Option<DateTimeOffset> = None

    let readData (wd : WorkingData) = 
        async {
          let fss, _ = wd.focusSessions.Value
          let sessions = 
                        fss
                            |> List.sortBy (fun (f : FocusSession) -> - f.startTime.Ticks)
                            |> List.filter (fun (f : FocusSession) -> (f.endTime - f.startTime).TotalMinutes >= 5.0 && f.idle = false)
          
          replayDate <- Some (DataDateRangeFilterUtils.endDt wd.configuration.dateRangeFilter)
          return DailyLogData(sessions, wd.configuration,  DataDateRangeFilterUtils.endDt wd.configuration.dateRangeFilter)
        }
    let showData (d : DailyLogData) =
        focusSessions.Clear()
        d.Sessions
           |> List.iter (fun f -> focusSessions.Add(DailyLogEntryViewModel(f, d.Config.classification.colorMap f.sessionClassId, d.Config.classification.classNames f.sessionClassId)))

        forDate <- Humanize.dateAndDay d.Date
        vm.TriggerPropertyChanged("SubTitle")
        
    member x.FocusSessions = focusSessions
    override x.ReadData _ wd = readData wd
    override x.ShowData d = showData d
    override x.Title = "Daily Log"
    override x.SubTitle = forDate
    override x.ReplayDate = replayDate
    


type DateTimeHumanizer() =
    inherit CastingOneWayConverterBase<System.DateTimeOffset, string>("")
    override x.Conv t = Humanize.time t


type DailyLogWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<DailyLogViewModel, DailyLogData>(wds, "DailyLogWidgetElement.xaml")
    override x.CreateViewModel wds = DailyLogViewModel(wds)

module DailyLogWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast DailyLogWidgetElement(wds)

