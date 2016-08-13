namespace BucklingSprings.Aware.Settings

open System
open System.IO
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open System.Windows.Media

open System.Collections.ObjectModel

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Measurement

open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Common.CatchUp
open BucklingSprings.Aware.Common.Summarizers
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Windows
open BucklingSprings.Aware.Input

open BucklingSprings.Aware.Core.Classifiers

open BucklingSprings.Classification

type MoreOfChoice(value : int) =
    let n = if value = 0 then
                        "n / a"
                    elif value = 1 then
                        "More"
                    else
                        "Less"
    member x.Value = value
    member x.Name = n
    override x.ToString () = n
    static member More = MoreOfChoice(1)
    static member Less = MoreOfChoice(-1)
    static member NA = MoreOfChoice(0)

type AssignableClassViewModel(id : int option, nm : string, moreOf : int,brush : Brush, p : float, assign) =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let mutable currentExample : (ActivityWindowDetail * int) option = None
    let mutable probability = p
    let mutable textProgram = String.Empty
    let mutable text = String.Empty
    let assignCommand = DelegatingCommand(fun () -> assign currentExample)
    member x.Category = nm
    member x.CategoryBrush = brush
    member x.Importance = probability
    member x.AssignCommand = assignCommand
    member x.Id = id
    member x.MoreOf = moreOf
    member x.ShowExample e p =
        currentExample <- e
        probability <- p
        assignCommand.ChangeCanExecuteTo (Option.isSome currentExample)
        propertyChanged.Trigger(x, PropertyChangedEventArgs("Importance"))
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)
        
    

type ClassViewModel(id : int option, nm : string, brush : Brush, addNewClassPlaceHolder, onClassDelete, onNameChange, onMoreOfChange, readOnlyMessage, isAssignable, moreOrLess) as vm =
    let moreOfChoices = ObservableCollection<MoreOfChoice>([MoreOfChoice.More; MoreOfChoice.Less; MoreOfChoice.NA])
    let editable = String.IsNullOrWhiteSpace(readOnlyMessage)
    let mutable isPlaceHolder = if id = None then true else false
    let mutable name = nm
    let mutable moreOf = moreOrLess
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    
    let deleteCommand = DelegatingCommand(fun () -> onClassDelete vm)
      
    do
        deleteCommand.ChangeCanExecuteTo true

    member x.Name
        with get() = name
        and set v =
            name <- v
            
            if isPlaceHolder = true then
                addNewClassPlaceHolder ()

            isPlaceHolder <- false
            onNameChange id v
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("DeleteVisibility"))

    member x.Brush = brush
    member x.DeleteCategory = deleteCommand
    member x.DeleteVisibility = if isPlaceHolder || (not editable) then Visibility.Hidden else Visibility.Visible
    member x.MoreOfVisibility = if editable then Visibility.Visible else Visibility.Hidden
    member x.PhrasesVisibility = if editable then Visibility.Visible else Visibility.Collapsed
    member x.HintVisibility = if editable then Visibility.Collapsed else Visibility.Visible
    member x.IsReadOnly = if editable then false else true
    member x.Id = id
    member x.Hint = readOnlyMessage
    member x.MoreOf
        with get() = moreOf
        and set (v : int) =
            moreOf <- v
            onMoreOfChange id v
    member x.MoreOfChoices = moreOfChoices
    

    member x.IsAssignable = isAssignable && (isPlaceHolder = false)

type DesignTimeCategorySettingsPageViewModel() =
    let classes = ObservableCollection<ClassViewModel>()
    let nop _ = ()
    let onNameChange _ _ = ()
    let onMoreOfChange _ _ = ()
    let b1 = List.head Theme.customColors
    let b2 = List.nth Theme.customColors 1
    let b3 = List.nth Theme.customColors 2
    let saveCommand = DelegatingCommand(nop)
    let trainCommand = DelegatingCommand(nop)
    let reTrainCommand = DelegatingCommand(nop)
    
    let enoughData = true

    do
        classes.Add(ClassViewModel(Some 3, "Play", b1.back, nop, nop, onNameChange, onMoreOfChange, String.Empty, true, -1))
        classes.Add(ClassViewModel(Some 3, "Learn", b1.back, nop, nop, onNameChange, onMoreOfChange, String.Empty, true, 1))
        classes.Add(ClassViewModel(Some 3, "Work", b1.back, nop, nop, onNameChange, onMoreOfChange, String.Empty, true, 1))
        classes.Add(ClassViewModel(Some 2, "Idle", Theme.idleColors.back, nop, nop, onNameChange, onMoreOfChange, "Category used when there is no keyboard or mouse activity or the computer is turned off.", true, 0))
        classes.Add(ClassViewModel(Some 1, "Other", Theme.otherColors.back, nop, nop, onNameChange, onMoreOfChange, "Category for activities that do not match any other category.", true, 0))

        saveCommand.ChangeCanExecuteTo true
        trainCommand.ChangeCanExecuteTo true
        reTrainCommand.ChangeCanExecuteTo false


    member x.MainTitle = "Categories"
    member x.Classes = classes
    member x.SaveCommand = saveCommand
    member x.TrainNewModelCommand = trainCommand
    member x.ReTrainCommand = reTrainCommand
    member x.NotEnoughDataHintVisibility = if enoughData then Visibility.Collapsed else Visibility.Visible
    member x.WelcomeSectionVisibility = Visibility.Visible
    member x.TrainSectionVisibility = Visibility.Collapsed


type ExampleViewModel(awd : ActivityWindowDetail) =
    member x.ProgramName = awd.processInformation.processName
    member x.Text = awd.windowText


[<AllowNullLiteral()>]
type TrainViewModel(classes : seq<ClassViewModel>, onCancel, onSave) as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let assignableClasses = ObservableCollection<AssignableClassViewModel>()
    let mutable textProgram = String.Empty
    let mutable text = String.Empty
    let cancelTraining = DelegatingCommand(onCancel)
    let m = ModelStore.create()
    let mutable exampleForUndo = None
    let mutable model = classes
                            |> Seq.map (fun c -> (c.Name, c.Id))
                            |> Set.ofSeq
                            |> Classifier.create
    let traceFw features w t c =
        let fs = Set.toSeq features
        let s = System.String.Join(", ", fs)
        Diagnostics.Trace.WriteLine(sprintf "%s - %s - %i : %s" t s w c)
        
    let pickByNameOrId (c,id) (a : AssignableClassViewModel) =
        if Option.isNone id then
             a.Category = c
        else
            a.Id = id
    let save () =
        let saveFeature = id
        let saveClassName c =
            match c with
            | (n, None) -> n
            | (n, Some _) -> n
        ModelSerialization.save model "ver1" saveFeature saveClassName (ModelStore.modelFileName m)
        let classes =
            assignableClasses |> Seq.map (fun c -> (c.Category, c.MoreOf)) |> Seq.toList
        ClassifierStore.saveWithModel m classes
        onSave()

    let nop () = ()
    let saveChanges = DelegatingCommand(save)
    let undoCommand = DelegatingCommand(nop)
    let showExample example =
        if Option.isSome example then
            let e,w = Option.get example
            let features = Set.ofList (Phrase.extractWords e)
            let probs = Classifier.predict features model
            
            probs
                |> Seq.iter (fun (c,p) -> 
                                let assignableClass = Seq.find (pickByNameOrId c) assignableClasses
                                assignableClass.ShowExample example p)
            textProgram <- e.processInformation.processName
            text <- e.windowText
        else
            assignableClasses
                |> Seq.iter (fun a -> a.ShowExample None 1.0)
            textProgram <- String.Empty
            text <- "No examples to train on!"
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("TextProgram"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("Text"))
    let undoer (c : ClassViewModel) (eo : (ActivityWindowDetail * int) option) =
        let u () =
            undoCommand.ChangeCanExecuteTo false
            undoCommand.DelegateTo(nop)
            match eo with
                | Some (e,w) -> 
                    let features = Set.ofList (Phrase.extractWords e)
                    traceFw features w "undo" (c.Name)
                    let m' = Classifier.undoWeighted features w (c.Name, c.Id) model
                    match m' with
                        | None -> ()
                        | Some undoneModel ->
                            model <- undoneModel
                            ExampleStore.markAsUnTrained m e
                            showExample  (Some (e,w))
                            
                | None -> ()
        u
    
    let assign (c : ClassViewModel) (eo : (ActivityWindowDetail * int) option) =
        saveChanges.ChangeCanExecuteTo true
        undoCommand.ChangeCanExecuteTo true
        undoCommand.DelegateTo(undoer c eo)
        match eo with
        | Some (e,w) ->
            
            let features = Set.ofList (Phrase.extractWords e)
            traceFw features w "train" (c.Name)
            model <- Classifier.trainWeighted features w (c.Name, c.Id) model
            ExampleStore.markAsTrained m e
            let example = ExampleStore.nextExample m
            showExample example
        | None -> ()
    let asAssignableClass (c : ClassViewModel) = 
        AssignableClassViewModel(c.Id, c.Name, c.MoreOf,c.Brush, 1.0, assign c)
    
        
    
        
        
    do
        let example = ExampleStore.nextExample m
        
        cancelTraining.ChangeCanExecuteTo true
        saveChanges.ChangeCanExecuteTo false
        classes
            |> Seq.sortBy (fun c -> c.Name)
            |> Seq.iter (fun c -> assignableClasses.Add(asAssignableClass c))

        showExample example
        
        
    member x.ClassChoices = assignableClasses
    member x.CancelTrainingCommand = cancelTraining
    member x.SaveChangesCommand = saveChanges
    member x.UndoCommand = undoCommand
    member x.TextProgram = textProgram
    member x.Text = text
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

type DesignTimeTrainViewModel() =
    
    let assignableClasses = ObservableCollection<AssignableClassViewModel>()
    let b1 = List.head Theme.customColors
    let b2 = List.nth Theme.customColors 1
    let b3 = List.nth Theme.customColors 2
    let nop _ = ()
    do
        assignableClasses.Add(AssignableClassViewModel(None, "Play", -1, b1.back, 1.0, nop))
        assignableClasses.Add(AssignableClassViewModel(None, "Learn", 1, b2.back, 0.9, nop))
        assignableClasses.Add(AssignableClassViewModel(None, "Work", 1, b3.back, 0.9, nop))
        assignableClasses.Add(AssignableClassViewModel(None, "Play Long Name", -1, b1.back, 0.6, nop))

    member x.ClassChoices = assignableClasses
    member x.TextProgram = "Google Chrome"
    member x.Text = "google - Google Search"
    

type CategorySettingsPageViewModel(cs : IConfigurationService, mds : IMessageService) as vm =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let mutable training : TrainViewModel = null
   
    let mutable classAddedOrDeleted = false
    let classes = ObservableCollection<ClassViewModel>()
    let nop _ = ()
    let cancelTraining () =
        training <- null
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("WelcomeSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("TrainSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("Training"))
    let trainCommand = DelegatingCommand(nop)
    let colorFor (x : ClassificationClass) =
        let ab = cs.CurrentConfiguration.classification.colorMap (ClassIdentifier x.id)
        ab.back
    let nextColor () =
        let color = Seq.nth classes.Count Theme.infiniteCustomColors
        color.back

    let resetCommands () =
        trainCommand.ChangeCanExecuteTo true

    let onNameChange id newName =
        if Option.isSome id then
            ClassifierStore.changeName (Option.get id) newName
    let onMoreOfChange id moreOf =
        if Option.isSome id then
            ClassifierStore.changeMoreOf (Option.get id) moreOf

    let rec addNewPlaceholder () =
            classAddedOrDeleted <- true
            classes.Add(ClassViewModel(None, String.Empty, nextColor (), addNewPlaceholder, nop, onNameChange, onMoreOfChange, System.String.Empty, true, 0))
            resetCommands()
    
    let onClassDelete c =
        classAddedOrDeleted <- true
        classes.Remove(c) |> ignore
        resetCommands()


    let init () =
        let nop _ = ()
        classAddedOrDeleted <- false
        training <- null
        classes.Clear()
        let categoryClassifierDef = ClassifierStore.loadCurrentCategoryCLassifierDefinition()
        categoryClassifierDef.Classes
            |> Seq.iter (fun c -> classes.Add(ClassViewModel(Some c.id, c.className, colorFor c, addNewPlaceholder, onClassDelete, onNameChange, onMoreOfChange, System.String.Empty, true, c.moreOf)))
        classes.Add(ClassViewModel(Some 2, "Idle", Theme.idleColors.back, addNewPlaceholder, nop, onNameChange, onMoreOfChange, "Category used when there is no keyboard or mouse activity or the computer is turned off.", false, 0))
        classes.Add(ClassViewModel(Some 1, "Other", Theme.otherColors.back, addNewPlaceholder, nop, onNameChange, onMoreOfChange, "Category for activities that do not match any other category.", true, 0))
        classes.Add(ClassViewModel(None, String.Empty, nextColor (), addNewPlaceholder, nop, onNameChange, onMoreOfChange, System.String.Empty, true, 0))
        resetCommands()
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("WelcomeSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("TrainSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("Training"))

    let onSave () =
        init ()
        mds.Display("The settings have been saved but might take a while to apply. You can check the progress by clicking on the 'settings' page.")
        // Reload global configuration so that the UI can be updated.
        Async.Start(cs.ReloadAsync())
        
    let trainNewModel () =
        let assignable = classes
                            |> Seq.filter (fun c -> c.IsAssignable = true)
        training <- TrainViewModel(assignable, cancelTraining, onSave)
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("WelcomeSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("TrainSectionVisibility"))
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("Training"))
        ()

    let helpCommand = AlwaysExecutableCommand(fun _ -> System.Diagnostics.Process.Start("http://www.aware.am/help/categories") |> ignore)
        
        
    do
        trainCommand.DelegateTo trainNewModel
        init()
        let catDone = ClassifierStore.recategorizationPercentageDone()
        if Option.isSome catDone then
            let percent = Option.get catDone
            mds.Display(sprintf "Activities are still being recategorized in the background (%d%% Remaining)." percent)
        else
            let summaryVersion = DailySummarizer.summarizerSetVersion (ClassifierStore.allClassifierVersions ())
            let dayCount = DailySummaryCatchup.numberOfDaysNeeding summaryVersion
            if dayCount > 0 then
                mds.Display(sprintf "Summarizing categorized data (%d days left)." dayCount)
            
        
    member x.MainTitle = "Categories"
    member x.Classes = classes
    member x.TrainNewModelCommand = trainCommand
    member x.HelpCommand = helpCommand
    member x.WelcomeSectionVisibility = if training = null then Visibility.Visible else Visibility.Collapsed
    member x.TrainSectionVisibility = if training = null then Visibility.Collapsed else Visibility.Visible
    member x.Training = training
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

type CategorySettingsPage(cs : IConfigurationService, mds : IMessageService) as pg =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/CategorySettingsPage.xaml", UriKind.Relative)) :?> UserControl
    do
        pg.Content <- content
        pg.Loaded.Add(fun  _ -> content.DataContext <- CategorySettingsPageViewModel(cs, mds))
        

