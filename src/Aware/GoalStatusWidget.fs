namespace BucklingSprings.Aware.Widgets

open BucklingSprings.Aware
open System.Windows

type GoalsViewModel(wds : WorkingDataService) =
    inherit WidgetViewModelBase<Unit>(wds, true)

   

    override x.ReadData initial (wd : WorkingData) =
        async {
            return ()
        }
    override x.ShowData _ = ()
    override x.Title = "Goals"


  



type GoalStatusWidgetElement(wds : WorkingDataService) =
    inherit StandardWidgetElementBase<GoalsViewModel, Unit>(wds, "GoalStatusWidgetElement.xaml")
    override x.CreateViewModel wds = GoalsViewModel(wds)
        



module GoalStatusWidgetWidgetFactory =
    let create : WorkingDataService -> UIElement = fun wds -> upcast GoalStatusWidgetElement(wds)