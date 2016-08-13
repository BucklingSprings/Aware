#nowarn "52"

namespace BucklingSprings.Aware

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.ActivitySamples
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Focus

open System

[<RequireQualifiedAccess()>]
[<NoComparison()>]
[<NoEquality()>]
type WorkingData = 
    {
        lastRefreshed : System.DateTimeOffset
        minimumDate : System.DateTimeOffset
        maxmimumDate : System.DateTimeOffset
        lastNDaySamplesForCurrentClassifier : WithDate<MeasureForClass<ActivitySummary>> list
        focusSessions : Lazy<FocusSession list * OnGoingFocusSession option>
        unfilteredStoredSummaries : StoredSummary list
        storedSummaries : Lazy<StoredSummaries>
        configuration : UserGlobalConfiguration
        historicalPerformance : DailyPerformance
        todaysPerformance : DailyPerformance
    }

module ApplicationBaseDataLoader =

    let bySummaryDate (s: StoredSummary) = s.summaryTimeAndDate
        
    let dateRange filter ss =
            match filter with
                | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> startDt,endDt
                | DataDateRangeFilter.NoFilter -> if List.isEmpty ss then
                                                    let now = System.DateTimeOffset.Now
                                                    now.AddDays(-7.0),now
                                                  else
                                                    let s, e = (ss |> List.minBy bySummaryDate |> bySummaryDate, ss |> List.maxBy bySummaryDate |> bySummaryDate)
                                                    if e.Subtract(s).TotalDays > 7.0 then s,e else e.SubtractDays(7),e
        
    let loadDailySamples  (config : UserGlobalConfiguration) =
        let endDate = match config.dateRangeFilter with
                        | DataDateRangeFilter.NoFilter -> System.DateTimeOffset.Now
                        | DataDateRangeFilter.FilterDataTo(_, e) -> e.EndOfDay

        let actS = ActivitySamplesStore.getLastNDaysOfActivities 7 endDate

        let classIdSet = Set(config.classification.selectedClassifier.classes |> Seq.map (fun c -> ClassIdentifier c.id))
        TypedActivitySamples.asTyped classIdSet actS


    let load (config : UserGlobalConfiguration) : WorkingData =
        let filterOnPeriodsByDateRange =
                match config.dateRangeFilter with
                | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> List.filter (fun (s : StoredSummary) -> s.summaryTimeAndDate >= startDt && s.summaryTimeAndDate <= endDt)
                | DataDateRangeFilter.NoFilter -> id

        let ss = SummaryStore.allSummaries() 

        let min, max = dateRange config.dateRangeFilter ss
        let typedSamples = loadDailySamples config
        let now = DateTimeOffset.Now
        

        {
            WorkingData.lastRefreshed = System.DateTimeOffset.Now
            WorkingData.minimumDate = min
            WorkingData.maxmimumDate = max
            WorkingData.lastNDaySamplesForCurrentClassifier = typedSamples
            WorkingData.configuration = config
            WorkingData.unfilteredStoredSummaries = ss
            WorkingData.storedSummaries = LazyTypedStoredSummariesReader.read (filterOnPeriodsByDateRange ss)
            WorkingData.focusSessions = LazyFocusSessions.focusSessionsForClassifier config typedSamples
            WorkingData.historicalPerformance = DailyTotalsStore.historicalPerformanceUsingDateRange (now.SubtractDays(30)) now
            WorkingData.todaysPerformance = DailyTotalsStore.dailyPerformanceUsingDateRange  (now.StartOfDay) now
        }

    let reLoad (config : UserGlobalConfiguration) (current : WorkingData) : WorkingData =

        let filterOnPeriodsByDateRange =
                match config.dateRangeFilter with
                | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> List.filter (fun (s : StoredSummary) -> s.summaryTimeAndDate >= startDt && s.summaryTimeAndDate <= endDt)
                | DataDateRangeFilter.NoFilter -> id
        
        let storedSummaries = current.unfilteredStoredSummaries |> filterOnPeriodsByDateRange
        let bySummaryDate (s: StoredSummary) = s.summaryTimeAndDate
        let min, max = dateRange config.dateRangeFilter current.unfilteredStoredSummaries
        let now = DateTimeOffset.Now

        let typedSamples = loadDailySamples config
        {
            WorkingData.lastRefreshed = System.DateTimeOffset.Now
            WorkingData.minimumDate = min
            WorkingData.maxmimumDate = max
            WorkingData.lastNDaySamplesForCurrentClassifier = typedSamples
            WorkingData.configuration = config
            WorkingData.unfilteredStoredSummaries = current.unfilteredStoredSummaries
            WorkingData.storedSummaries = LazyTypedStoredSummariesReader.read (filterOnPeriodsByDateRange current.unfilteredStoredSummaries)
            WorkingData.focusSessions = LazyFocusSessions.focusSessionsForClassifier config typedSamples
            WorkingData.historicalPerformance = DailyTotalsStore.historicalPerformanceUsingDateRange (now.SubtractDays(30)) now
            WorkingData.todaysPerformance = DailyTotalsStore.dailyPerformanceUsingDateRange (now.StartOfDay) now
        }

    let refresh (config : UserGlobalConfiguration) (current : WorkingData) : WorkingData =
        let now = DateTimeOffset.Now
        let typedSamples = loadDailySamples config
        {
            current with
                        WorkingData.lastNDaySamplesForCurrentClassifier = typedSamples; focusSessions = LazyFocusSessions.focusSessionsForClassifier config typedSamples
                        WorkingData.todaysPerformance = DailyTotalsStore.historicalPerformanceUsingDateRange (now.StartOfDay) now}


type WorkingDataChangedEventArgs(wd  : WorkingData) =
    inherit System.EventArgs()
    member x.NewWorkingData : WorkingData = wd

type WorkingDataRefreshedEventArgs(wd  : WorkingData) =
    inherit System.EventArgs()
    member x.RefreshedData : WorkingData = wd

type WorkingDataService(cs : IConfigurationService) =
    let mutable workingData = ApplicationBaseDataLoader.load cs.CurrentConfiguration
    let workingDataChanged = Event<WorkingDataChangedEventArgs>()
    let workingDataRefreshed = Event<WorkingDataRefreshedEventArgs>()

    let reloadForWithNewConfiguration config = 
        workingData <- ApplicationBaseDataLoader.reLoad cs.CurrentConfiguration workingData
        workingDataChanged.Trigger(WorkingDataChangedEventArgs(workingData))
        workingDataRefreshed.Trigger(WorkingDataRefreshedEventArgs(workingData))

    do
        cs.ConfigurationChanged.Add(fun e -> reloadForWithNewConfiguration e.NewConfiguration)
        workingDataRefreshed.Trigger(WorkingDataRefreshedEventArgs(workingData))

    member x.Refresh () =
        let timeSinceLastRefresh = System.DateTimeOffset.Now - workingData.lastRefreshed
        if cs.CurrentConfiguration.classification.alwaysReloadConfiguration then
            cs.ReloadAsync()
        else
            async {
                if timeSinceLastRefresh.TotalSeconds > 5.0 then
                    workingData <- ApplicationBaseDataLoader.refresh cs.CurrentConfiguration workingData
                    workingDataRefreshed.Trigger(WorkingDataRefreshedEventArgs(workingData))
                return ()
            }
        

    member x.WorkingData = workingData
    member x.WorkingDataChanged : System.IObservable<WorkingDataChangedEventArgs> = upcast workingDataChanged.Publish
    member x.WorkingDataRefreshed : System.IObservable<WorkingDataRefreshedEventArgs> = upcast workingDataRefreshed.Publish
    member x.ConfigurationService = cs

