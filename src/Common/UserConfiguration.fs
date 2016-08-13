namespace BucklingSprings.Aware.Common.UserConfiguration

open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Utils
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Common.Themes
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions


[<RequireQualifiedAccess()>]
[<NoComparison()>]
type ClassificationClassFilter = 
    | NoFilter
    | FilterToClasses of ClassificationClass list

[<NoComparison()>]
[<RequireQualifiedAccess()>]
[<NoEquality()>]
type ClassificationConfiguration =
    {
        visibleClasses: ClassificationClass list
        colorMap : ClassIdentifier -> AssignedBrushes
        classNames : ClassIdentifier -> string
        moreOfMap : ClassIdentifier -> MoreOf
        idleMap : ClassIdentifier -> bool
        filter: ClassificationClassFilter
        selectedClassifier : Classifier
        classifiers : Classifier list
        alwaysReloadConfiguration : bool
    }



type ApplyToOverview = bool

[<RequireQualifiedAccess()>]
type DataDateRangeFilter =
    | NoFilter
    | FilterDataTo of System.DateTimeOffset * System.DateTimeOffset


[<NoComparison()>]
[<NoEquality()>]
type UserGlobalConfiguration =
    {
        classification : ClassificationConfiguration;
        dateRangeFilter : DataDateRangeFilter;}


type UserConfigurationChangedEventArgs(gc : UserGlobalConfiguration) =
    inherit System.EventArgs()
    member x.NewConfiguration : UserGlobalConfiguration = gc

type IConfigurationService =
    abstract CurrentConfiguration : UserGlobalConfiguration
    abstract UpdateClassificationFilterAsync : Classifier -> ClassificationClassFilter -> Async<Unit>
    abstract UpdateDateRangeAsync : DataDateRangeFilter -> Async<Unit>
    abstract ConfigurationChanged : System.IObservable<UserConfigurationChangedEventArgs>
    abstract ConfigurationReloaded : System.IObservable<UserConfigurationChangedEventArgs>
    abstract ReloadAsync : Unit -> Async<Unit>



module ConfigurationLoader =

    let visibleClassesAndColors (c : Classifier) : (ClassificationClass * AssignedBrushes) list =
        let namedClasses = ClassifierStore.namedClassesInOrderOfUse c
        namedClasses |> List.iter (fun clas -> clas.classifier <- c)
        let systemClassAndColor (cc: ClassificationClass) =
            if cc.idle then
                Some (cc, Theme.idleColors)
            elif cc.catchAll then
                Some (cc, Theme.otherColors)
            else
                None
        let systemClasses = c.classes |> Seq.toList |> List.choose systemClassAndColor
        List.append (Seq.zip namedClasses Theme.infiniteCustomColors |> Seq.toList) systemClasses


        
    
    let loadClassificationConfiguration () =
        let classifiers = ClassifierStore.allClassifiers()
        let visibleClassesAndColors = List.collect visibleClassesAndColors classifiers
        let nonSystemClassCount = 
            visibleClassesAndColors
                |> List.filter (fun (c,_) -> c.idle = false && c.catchAll = false)
                |> Seq.length
        
        let allClasses = ClassifierStore.allClasses()
        let idleClasses = Set(allClasses|> List.choose (fun (c : ClassificationClass) ->  if c.idle then Some (ClassIdentifier c.id) else None))
        let names = Map(allClasses |> List.map (fun (c : ClassificationClass) -> (ClassIdentifier c.id, c.className)))
        let moreOf (m : int) =
            if m = 0 then
                MoreOf.Neutral
            elif m > 0 then
                MoreOf.MoreOf
            else
                MoreOf.LessOf
        let moreOfs = Map(allClasses |> List.map (fun (c : ClassificationClass) -> (c.id,moreOf c.moreOf)))
        let colors = Map(visibleClassesAndColors |> List.map (fun (cc, color) -> (cc.id, color)))
        let visibleClasses = visibleClassesAndColors |> List.map fst
        let colorMap (cc : ClassificationClass) =
            if cc = null then
                Theme.otherColors
            elif colors.ContainsKey(cc.id) then
                colors.Item(cc.id)
            else
                Theme.otherColors
        let moreOfMap (ClassIdentifier(cc)) =
            if moreOfs.ContainsKey(cc) then
                moreOfs.Item(cc)
            else
                MoreOf.Neutral
        let colorMap' (ClassIdentifier(cc)) =
            if colors.ContainsKey(cc) then
                colors.Item(cc)
            else
                Theme.otherColors

        let classNameMap (cc) =
            if names.ContainsKey(cc) then
                names.Item(cc)
            else
                "Unknown"
        let idleMap (cc) = idleClasses.Contains(cc)
        {
            ClassificationConfiguration.visibleClasses = visibleClasses
            ClassificationConfiguration.colorMap = colorMap'
            ClassificationConfiguration.classNames = classNameMap
            ClassificationConfiguration.moreOfMap = moreOfMap
            ClassificationConfiguration.idleMap = idleMap
            ClassificationConfiguration.filter = ClassificationClassFilter.NoFilter
            ClassificationConfiguration.classifiers = classifiers
            ClassificationConfiguration.selectedClassifier = List.head classifiers;
            ClassificationConfiguration.alwaysReloadConfiguration = nonSystemClassCount < 5
        }

    let loadConfiguration () =
        let classificationCfg = loadClassificationConfiguration()
        {classification = classificationCfg; dateRangeFilter = DataDateRangeFilter.NoFilter}

    
module EmptyConfiguration =
    let empty = 
        let classificationConfiguration = {
                                        ClassificationConfiguration.visibleClasses = []
                                        ClassificationConfiguration.classifiers = []
                                        ClassificationConfiguration.filter = ClassificationClassFilter.NoFilter
                                        ClassificationConfiguration.colorMap = fun _ -> Theme.otherColors
                                        ClassificationConfiguration.idleMap = fun _ -> false
                                        ClassificationConfiguration.classNames = fun _ -> ""
                                        ClassificationConfiguration.moreOfMap = fun _ -> MoreOf.Neutral
                                        ClassificationConfiguration.selectedClassifier = Classifier()
                                        ClassificationConfiguration.alwaysReloadConfiguration = true}
        {classification = classificationConfiguration; dateRangeFilter = DataDateRangeFilter.NoFilter}

type ConfigurationNotLoadedConfigurationService() =
    let configurationChanged = Event<UserConfigurationChangedEventArgs>()
    let currentConfig = EmptyConfiguration.empty
    interface IConfigurationService with
        member x.CurrentConfiguration : UserGlobalConfiguration  = currentConfig
        member x.UpdateClassificationFilterAsync _ _ = async { return ()}
        member x.UpdateDateRangeAsync _ =  async { return ()}
        member x.ConfigurationChanged = configurationChanged.Publish :> System.IObservable<UserConfigurationChangedEventArgs>
        member x.ConfigurationReloaded = configurationChanged.Publish :> System.IObservable<UserConfigurationChangedEventArgs>
        member x.ReloadAsync() =  async { return ()}

type ConfigurationService() =
    let lockToken = System.Object()
    let configurationChanged = Event<UserConfigurationChangedEventArgs>()
    let configurationReloaded = Event<UserConfigurationChangedEventArgs>()
    let mutable currentConfig : UserGlobalConfiguration = ConfigurationLoader.loadConfiguration()
    let updateCurrentConfig (cfg : UserGlobalConfiguration) =
        async {
            lock lockToken (
                            fun () ->
                                        currentConfig <- cfg
                                        configurationChanged.Trigger(UserConfigurationChangedEventArgs(cfg)))}
    interface IConfigurationService with
        member x.CurrentConfiguration : UserGlobalConfiguration  = currentConfig
         member x.UpdateClassificationFilterAsync classifier f = 
            let newClassificationConfig = {currentConfig.classification with filter = f; selectedClassifier = classifier}
            let newConfig = {currentConfig with classification = newClassificationConfig}
            updateCurrentConfig newConfig
        member x.UpdateDateRangeAsync d = 
            let newConfig = {currentConfig with dateRangeFilter = d}
            updateCurrentConfig newConfig
        member x.ConfigurationChanged = configurationChanged.Publish :> System.IObservable<UserConfigurationChangedEventArgs>
        member x.ConfigurationReloaded = configurationReloaded.Publish :> System.IObservable<UserConfigurationChangedEventArgs>
        member x.ReloadAsync() =  async { 
                                    lock lockToken (
                                                    fun () ->
                                                        currentConfig <- ConfigurationLoader.loadConfiguration()
                                                        configurationChanged.Trigger(UserConfigurationChangedEventArgs(currentConfig))
                                                        configurationReloaded.Trigger(UserConfigurationChangedEventArgs(currentConfig)))
                                    return ()
                                  }

    
module ClassificationClassFilterUtils = 

    let selectedClassesOrAll (clsCfg : ClassificationConfiguration) : ClassificationClass list =
        match clsCfg.filter with
        | ClassificationClassFilter.NoFilter ->
            let classifier = clsCfg.selectedClassifier
            classifier.classes |> List.ofSeq |> List.filter (fun c -> c.idle = false)
        | ClassificationClassFilter.FilterToClasses cls -> cls

    let map (nofilter : 'a) (forClass : ClassificationClass -> 'a)  (cf  : ClassificationClassFilter)  : 'a list =
        match cf with
        | ClassificationClassFilter.NoFilter -> [nofilter]
        | ClassificationClassFilter.FilterToClasses cls -> cls |> List.map (fun c -> forClass c)


module DataDateRangeFilterUtils =

    let isToday dr =
        match dr with
        | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> endDt.IsSameDay(System.DateTimeOffset.Now)
        | DataDateRangeFilter.NoFilter -> true

    let endDt dr =
        match dr with
        | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> endDt
        | DataDateRangeFilter.NoFilter -> System.DateTimeOffset.Now
    let formatted dr min max =
        match dr with
        | DataDateRangeFilter.FilterDataTo(startDt, endDt) -> sprintf "%s - %s" (Humanize.dateAndDay startDt) (Humanize.dateAndDay endDt)
        | DataDateRangeFilter.NoFilter -> sprintf "%s - %s" (Humanize.dateAndDay min) (Humanize.dateAndDay max)

    let formattedEndDt dr  =
        match dr with
        | DataDateRangeFilter.FilterDataTo(_, endDt) -> Humanize.dateAndDay endDt
        | DataDateRangeFilter.NoFilter -> (Humanize.dateAndDay System.DateTimeOffset.Now)
