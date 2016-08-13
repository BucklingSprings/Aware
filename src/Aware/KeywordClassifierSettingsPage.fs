namespace BucklingSprings.Aware.Settings

open System
open System.ComponentModel
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Collections.ObjectModel

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Windows
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Store.Settings
open BucklingSprings.Aware.Input
open BucklingSprings.Aware.Core.Settings
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Themes

type KeywordViewModel(p : string, onDelete, onPlaceHolderUsed, onAnyChange) as vm =
    let mutable phrase = p
    let mutable isPlaceHolder = String.IsNullOrWhiteSpace(phrase)
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    member x.Phrase 
        with get () = phrase
        and set v =
            onAnyChange()
            phrase <- v
            if isPlaceHolder then onPlaceHolderUsed()
            isPlaceHolder <- false
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("Phrase"))
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("EditorVisibility"))
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("PhraseVisibility"))

    member x.DeletePhrase = AlwaysExecutableCommand(fun () -> 
                                                                onAnyChange()
                                                                onDelete vm)
    member x.EditorVisibility = if isPlaceHolder then Visibility.Visible else Visibility.Collapsed
    member x.PhraseVisibility = if isPlaceHolder then Visibility.Collapsed else Visibility.Visible
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

type KeywordClassViewModel(id : int option, nm : string, brush : Brush, ps : string list, addNewClassPlaceHolder, onClassDelete, onAnyChange, readOnlyMessage)  as vm=
    let editable = String.IsNullOrWhiteSpace(readOnlyMessage)
    let mutable isDirty = false
    let mutable isPlaceHolder = if id = None then true else false
    let mutable name = nm
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let phrases = ObservableCollection<KeywordViewModel>()
    let deleteCommand = DelegatingCommand(fun () -> onClassDelete vm)
    let deletePhrase existing =
                    phrases.Remove(existing) |> ignore

            
    let rec addPlaceHolderPhrase () =
        phrases.Add(KeywordViewModel(String.Empty, deletePhrase, addPlaceHolderPhrase, onAnyChange))

        
    do
        ps |> List.iter (fun p -> phrases.Add(KeywordViewModel(p, deletePhrase, addPlaceHolderPhrase, onAnyChange)))
        if not isPlaceHolder then addPlaceHolderPhrase()
        deleteCommand.ChangeCanExecuteTo true

    member x.Name
        with get() = name
        and set v =
            name <- v
            
            if isPlaceHolder = true then
                addPlaceHolderPhrase()
                addNewClassPlaceHolder ()

            isPlaceHolder <- false
            isDirty <- true
            onAnyChange()
            propertyChanged.Trigger(vm, PropertyChangedEventArgs("DeleteVisibility"))

    member x.Brush = brush
    member x.Phrases = phrases
    member x.DeleteCategory = deleteCommand
    member x.DeleteVisibility = if isPlaceHolder || (not editable) then Visibility.Hidden else Visibility.Visible
    member x.PhrasesVisibility = if editable then Visibility.Visible else Visibility.Collapsed
    member x.HintVisibility = if editable then Visibility.Collapsed else Visibility.Visible
    member x.IsReadOnly = if editable then false else true
    member x.Id = id
    member x.Hint = readOnlyMessage
    
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)


type DesignTimeKeywordSettingsPageViewModel() =
    let currentClasses = ObservableCollection<KeywordClassViewModel>()
    let onAdd _ = ()
    let onDelete _ = ()
    let onPhraseChange _ = ()
    do
        currentClasses.Add (KeywordClassViewModel(None, "Work", Theme.awareBrush,["Foo"; "Bar"; "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"; ""], onAdd, onDelete, onPhraseChange, System.String.Empty))
        currentClasses.Add (KeywordClassViewModel(None, "Social", Theme.awareBrush,["Baz"; "Boo"; ""], onAdd, onDelete, onPhraseChange, System.String.Empty))
        currentClasses.Add (KeywordClassViewModel(None, "Other", Theme.otherColors.back,[], onAdd, onDelete, onPhraseChange, "Catch all Class"))


    member x.CurrentClasses = currentClasses



type KeywordSettingsPageViewModel(cs : IConfigurationService, mds : IMessageService) as vm =
    let mutable isDirty = false
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let currentClasses = ObservableCollection<KeywordClassViewModel>()
    let saveCommand = DelegatingCommand(fun () -> ())
    let colorFor (x : ClassificationClass) =
        let ab = cs.CurrentConfiguration.classification.colorMap (ClassIdentifier x.id)
        ab.back
    let nextColor () =
        let color = Seq.nth currentClasses.Count Theme.infiniteCustomColors
        color.back
    let onAnyChange () =
        isDirty <- true
        saveCommand.ChangeCanExecuteTo true
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("SaveVisibility"))
    let onClassDelete c =
        isDirty <- true
        saveCommand.ChangeCanExecuteTo true
        currentClasses.Remove(c) |> ignore
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("SaveVisibility"))
    let rec addNewPlaceholder () =
            currentClasses.Add(KeywordClassViewModel(None, String.Empty, nextColor () , [], addNewPlaceholder, onClassDelete, onAnyChange, System.String.Empty))

    let init () =
        isDirty <- false
        saveCommand.ChangeCanExecuteTo false
        currentClasses.Clear()
        let categoryClassifierDef = ClassifierStore.loadCurrentCategoryCLassifierDefinition()
        categoryClassifierDef.Classes
            |> Seq.iter (fun c -> currentClasses.Add(KeywordClassViewModel(Some c.id, c.className, colorFor c, [], addNewPlaceholder, onClassDelete, onAnyChange, System.String.Empty)))
        currentClasses.Add(KeywordClassViewModel(None, "Other", Theme.otherColors.back, [], addNewPlaceholder, onClassDelete, onAnyChange, "Category for activities that do not match any keywords."))
        currentClasses.Add(KeywordClassViewModel(None, "Idle", Theme.idleColors.back, [], addNewPlaceholder, onClassDelete, onAnyChange, "Category used when there is no keyboard or mouse activity or the computer is turned off."))
        addNewPlaceholder()
        propertyChanged.Trigger(vm, PropertyChangedEventArgs("SaveVisibility"))


    do
        saveCommand.DelegateTo(fun () -> 
            let classesToSave = currentClasses
                                    |> Seq.map (fun c -> (c.Name, 
                                                                c.Phrases
                                                                    |> List.ofSeq
                                                                    |>  List.filter (fun p -> String.IsNullOrWhiteSpace(p.Phrase) |> not)
                                                                    |> List.map (fun p -> p.Phrase)))
                                    |> Seq.filter (fun (name,_) -> String.IsNullOrWhiteSpace(name) |> not )
                                    |> Seq.filter (fun (_,phrases) -> Seq.isEmpty phrases |> not)
                                    |> List.ofSeq

            ClassifierStore.saveNewCategorySettings classesToSave
            init()
            mds.Display("The settings have been saved but might take a while to apply. You can check the progress by clicking on the 'settings' page.")
            Async.Start(cs.ReloadAsync()))

        init()

    member x.CurrentClasses = currentClasses
    member x.Save = saveCommand
    member x.SaveVisibility = if isDirty then Visibility.Visible else Visibility.Collapsed
    [<CLIEvent>]
    member this.PropertyChanged  = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)
    

type KeywordClassifierSettingsPage(cs : IConfigurationService, mds : IMessageService) as pg =
    inherit Page()
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware;component/KeywordClassifierSettingsPage.xaml", UriKind.Relative)) :?> UserControl
    do
        pg.Content <- content
        pg.Loaded.Add(fun _ -> content.DataContext <- KeywordSettingsPageViewModel(cs, mds))