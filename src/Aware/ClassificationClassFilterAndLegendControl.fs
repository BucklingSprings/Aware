namespace BucklingSprings.Aware.Controls.Composite

open System
open System.Windows
open System.ComponentModel
open System.Windows.Controls
open System.Collections.ObjectModel

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes

type SelectableClassificationClass(cc : ClassificationClass, brushes : AssignedBrushes, selectionChanged) =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let mutable selected = false
    let mutable isActive = true
    member val Description = cc.className with get
    member val Foreground = brushes.fore with get
    
    member val ClassificationClass = cc with get
    member x.UnselectIfSelected () =
        if selected then
            selected <- false
            propertyChanged.Trigger(x, PropertyChangedEventArgs("IsSelected"))
    member x.Background
        with get () = if isActive then brushes.back else Theme.idleColors.back
    member x.IsSelected
        with get () = selected
        and set (v) =
            selected <- v
            selectionChanged()
    member x.ChangeActive b = 
        isActive <- b
        propertyChanged.Trigger(x, PropertyChangedEventArgs("Background"))

    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

type SelectableClassification(c : Classifier,ccs : ClassificationClass seq, colorMap, selectionChanged) as sc =
    let selectionChanged' () =
        let selectedClasses = sc.ClassificationClasses
                                |> Seq.filter (fun (c : SelectableClassificationClass) -> c.IsSelected) 
                                |> Seq.map (fun (c : SelectableClassificationClass) -> c.ClassificationClass)
                                |> Seq.toList
        selectionChanged c selectedClasses
    let visibleClasses = ObservableCollection<SelectableClassificationClass>(Seq.map (fun c -> SelectableClassificationClass(c, colorMap (ClassIdentifier c.id), selectionChanged')) ccs)

    member val ClassificationClasses = visibleClasses with get
    member val Description = c.classifierName
    member val Classifier = c
    member x.UnselectAll () = visibleClasses |> Seq.iter (fun c -> c.UnselectIfSelected())
    member x.SwitchToClassifier = AlwaysExecutableCommand(fun () -> selectionChanged c List.empty)
    member x.ChangeActive b = visibleClasses |> Seq.iter (fun c -> c.ChangeActive b)

type ClassificationClassFilterAndLegendViewModel(configurationService : IConfigurationService) =
    let classificationCfg = configurationService.CurrentConfiguration.classification
    let classifiers = ObservableCollection<SelectableClassification>()
    let classfierSort (c : Classifier) =
        if c.classifierType = PrimitiveConstants.categoryClassifierType then
            1
        elif c.classifierType = PrimitiveConstants.programClassifierType then
            2
        else
            3
        
    let classifiersAndClasses = classificationCfg.visibleClasses
                                    |> Seq.groupBy (fun c -> c.classifier)
                                    |> Seq.sortBy (fun (c,_) -> classfierSort c)

    let activate (cls : Classifier) =
        classifiers |> Seq.iter (fun c -> c.ChangeActive (c.Classifier.id = cls.id))
    do
        let selectionChanged (c : Classifier) (selectedClasses : ClassificationClass list) = 
            
            let filter = if List.isEmpty selectedClasses then
                            classifiers |> Seq.iter (fun c -> c.UnselectAll())
                            ClassificationClassFilter.NoFilter
                         else
                            classifiers |> Seq.filter (fun cls -> cls.Classifier <> c) |> Seq.iter (fun c -> c.UnselectAll())
                            ClassificationClassFilter.FilterToClasses selectedClasses
            activate c
            Async.Start(configurationService.UpdateClassificationFilterAsync c filter)

        for c, ccs in classifiersAndClasses do
            classifiers.Add(SelectableClassification(c, ccs, classificationCfg.colorMap, selectionChanged))
        activate classificationCfg.selectedClassifier
        

    member x.Classifiers = classifiers

type ClassificationClassFilterAndLegendControl() as ctl =
    inherit UserControl()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/ClassificationClassFilterAndLegendControl.xaml", UriKind.RelativeOrAbsolute)) :?> UserControl
    let redraw () =
        let cs : IConfigurationService = ctl.ConfigurationService
        content.DataContext <- ClassificationClassFilterAndLegendViewModel(cs)
        ()
    do
        content.DataContext <- ClassificationClassFilterAndLegendViewModel(ConfigurationNotLoadedConfigurationService())
        ctl.Content <- content
        

    static let RedrawOnConfigurationServiceChanged (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? ClassificationClassFilterAndLegendControl as c -> c.Redraw()
        | _ -> ()
    static let ConfigurationServiceProperty =
        DependencyProperty.Register(
                                     "ConfigurationService",
                                     typeof<IConfigurationService>,
                                     typeof<ClassificationClassFilterAndLegendControl>,
                                     new PropertyMetadata(
                                        ConfigurationNotLoadedConfigurationService(), 
                                        new PropertyChangedCallback(RedrawOnConfigurationServiceChanged)))

    member x.Redraw () = ctl.Dispatcher.Invoke(fun _ -> redraw())
    member x.ConfigurationService
        with get() = x.GetValue(ConfigurationServiceProperty) :?> IConfigurationService
        and  set(v : IConfigurationService) = x.SetValue(ConfigurationServiceProperty, v)
