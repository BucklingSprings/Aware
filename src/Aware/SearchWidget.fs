namespace BucklingSprings.Aware.Widgets


open System
open System.Windows

open BucklingSprings.Aware
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Widgets
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.ActivitySamples

type SearchViewModel(wds : WorkingDataService) as vm =
    inherit WidgetViewModelBase<Unit>(wds, false)
    let defaultSearchResult  = SearchResult(keyboardActivity = 0, seconds = 0)
    let mutable searchResult = defaultSearchResult
    let mutable searchTerm : String = String.Empty
    let searchText (t : string) =
        let dr = wds.ConfigurationService.CurrentConfiguration.dateRangeFilter
        match dr with
            | DataDateRangeFilter.NoFilter -> SearchStore.activityTime t
            | DataDateRangeFilter.FilterDataTo (s, e) -> SearchStore.activityTimeForDateRange s e t

        
    let search () =
        searchResult <- if String.IsNullOrWhiteSpace searchTerm then defaultSearchResult else (searchText searchTerm)
        vm.TriggerPropertyChanged("Words")
        vm.TriggerPropertyChanged("Minutes")
    member x.Minutes   = 
        let ts = TimeSpan.FromSeconds(float searchResult.seconds)
        int ts.TotalMinutes


    member x.Words   =  TypedActivitySamples.keyStrokesToWords (searchResult.keyboardActivity)

    member x.SearchCommand = AlwaysExecutableCommand(search)
    member x.SearchTerm
        with get () = searchTerm
        and set (v) =
            searchTerm <- v

    override x.ReadData _ wd = async { return () }
    override x.ShowData d =
        search ()
    override x.Title = "Search"
    override x.SubTitle = searchTerm



type SearchWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<SearchViewModel, Unit>(wds, "SearchWidgetElement.xaml")
    override x.CreateViewModel wds = SearchViewModel(wds)

module SearchWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast SearchWidgetElement(wds)