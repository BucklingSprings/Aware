namespace BucklingSprings.Aware.Windows.Layout

open BucklingSprings.Aware
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Windows
open BucklingSprings.Aware.Windows.Pages
open BucklingSprings.Aware.Widgets
open BucklingSprings.Aware.Settings

open System.Windows.Controls

[<NoComparison()>]
[<RequireQualifiedAccess()>]
type Navigatable = 
    {
        name : string
        navigateTo : Page
        keepFilters : bool
    }

module LayoutConfiguration =

    let devDefaultOverviewPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        p.Add(TimerWidgetWidgetFactory.create wds)
        p

    let defaultOverviewPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        p.Add(TimerWidgetWidgetFactory.create wds)
        p.Add(DailyLogWidgetFactory.create wds)
        p.Add(VisualLogWidgetFactory.create wds)
        p.Add(DailyUsageWidgetWidgetFactory.create wds)
        p.Add(TotalUsageWidgetWidgetFactory.create wds)
        p

    let defaultTrendsAndSummariesPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        p.Add(ProductivityTrendsWidgetFactory.create wds)
        p.Add(ProductivityDistributionWidgetFactory.create wds)
        p.Add(StartEndTimeTrendsWidgetFactory.create wds)
        p.Add(StartEndTimeDistributionWidgetFactory.create wds)
        p

    let defaultWorkingPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        p.Add(SearchWidgetWidgetFactory.create wds)
        p

    let defaultUnfinishedPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        p.Add(GoalStatusWidgetWidgetFactory.create wds)
        p.Add(NoOpScatterPlotWidgetFactory.create wds)
        p

    let defaultSettingsPage (wds : WorkingDataService) (mds : IMessageService) = 
        let p = SettingsHost()
        p.AddSettings "Goals" (lazy (GoalSettingsPage(wds) :> Page))
        p.AddSettings "Categories" (lazy (CategorySettingsPage(wds.ConfigurationService, mds) :> Page))
        p.AddSettings "Experiments" (lazy (ExperimentSettingsPage() :> Page))
        p.AddSettings "Replay" (lazy (DeepInspectionSettingsPage() :> Page))
        
        p

    let defaultInsightsPage (wds : WorkingDataService) = 
        let p = WidgetHost()
        
        p.Add(ProductivityByDayOfWeekWidgetFactory.create wds)
        p.Add(TrellisWordsWidgetWidgetFactory.create wds)
        p.Add(TrellisMinutesWidgetWidgetFactory.create wds)
        p.Add(TrellisWordsPerMinutesWidgetWidgetFactory.create wds)
        p


    let pages (wds : WorkingDataService) (mds : IMessageService) : Navigatable list =
        if Environment.currentEnvironment = Environment.Development then

            [
                {Navigatable.name = "Overview"; Navigatable.navigateTo =  defaultOverviewPage wds; keepFilters = true}
                {Navigatable.name = "Trends"; Navigatable.navigateTo = defaultTrendsAndSummariesPage wds; keepFilters = true}
                {Navigatable.name = "Settings"; Navigatable.navigateTo = defaultSettingsPage wds mds; keepFilters = false}
            ]
        else
            [
                {Navigatable.name = "Overview"; Navigatable.navigateTo =  defaultOverviewPage wds; keepFilters = true}
                {Navigatable.name = "Trends"; Navigatable.navigateTo = defaultTrendsAndSummariesPage wds; keepFilters = true}
                //{Navigatable.name = "Insights"; Navigatable.navigateTo = defaultInsightsPage wds; keepFilters = true}
                {Navigatable.name = "Settings"; Navigatable.navigateTo = defaultSettingsPage wds mds; keepFilters = false}
            ]
        