namespace BucklingSprings.Aware.Store

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Classifiers
open BucklingSprings.Aware.Entitities

open System.Data.Entity
open System.Data.Entity.Migrations
open System.Data
open System.Collections.Generic
open System.Linq

module ClassifierReader = 
    
    let extractCatchAllClass (classifier : Classifier) =
        let catchAll = classifier.classes |> Seq.find (fun c -> c.catchAll)
        catchAll

    let extractIdleClass (classifier : Classifier) =
        let catchAll = classifier.classes |> Seq.find (fun c -> c.idle)
        catchAll


[<AbstractClass()>]
type ClassifierDefinitionLoaderBase<'a when 'a :> IClassifierDefinition>(oldClassifier : Classifier, needsReload, reload) =
    let mutable classifier = oldClassifier
    let mutable lastRead : 'a option = None
    abstract Read : Classifier -> ClassificationClass -> ClassificationClass list -> 'a
    interface IClassifierDefinitionLoader<'a> with
        member x.ReadLatest =
            if Option.isNone lastRead || needsReload classifier then
                classifier <- reload oldClassifier
                let namedClasses = classifier.classes |> Seq.filter (fun c -> c.catchAll = false && c.idle = false && c.deleted = false) |> Seq.toList
                let idleClass = ClassifierReader.extractIdleClass classifier
                let d = x.Read classifier idleClass namedClasses
                lastRead <- Some d
            Option.get lastRead
        member x.SaveNewClass c =
            use ctx = new AwareContext(Store.connectionString)
            ctx.Classifiers.Attach(classifier) |> ignore
            classifier.classes |> Seq.iter (fun c -> ctx.Classes.Attach(c) |> ignore)
            c.classifier <- classifier
            classifier.classes.Add(c) |> ignore
            let saved = ctx.Classes.Add(c)
            ctx.SaveChanges() |> ignore
            lastRead <- None // Force a reload since the old information is nouw out of date.
            saved


type CategoryClassifierDefinitionLoader (classifier : Classifier, needsReload, reload) =
    inherit ClassifierDefinitionLoaderBase<CategoryClassifierDefinition>(classifier, needsReload, reload)
    
    override  x.Read c idleClass namedClasses = 
        let chatchAll = ClassifierReader.extractCatchAllClass c
        let m = if c.model = null then
                    None
                else
                    let f = ModelStore.modelFileName c.model
                    let readers version =
                        let fReader = id
                        let cReader = id
                        fReader, cReader
                    Some (BucklingSprings.Classification.ModelSerialization.load f readers)
        CategoryClassifierDefinition(c, idleClass, chatchAll, namedClasses, m)

type ProgramClassifierDefinitionLoader (classifier : Classifier, needsReload, reload) =
    inherit ClassifierDefinitionLoaderBase<ProgramClassifierDefinition>(classifier, needsReload, reload)
    override  x.Read c idleClass namedClasses = 
        let programToClassMap = namedClasses |> List.map (fun c -> (c.className, c))
        ProgramClassifierDefinition(c, idleClass, Map(programToClassMap))


module ClassifierStore = 

    let changeName (classId : int) (newName : string) =
        use ctx = new AwareContext(Store.connectionString)
        let cls = ctx.Classes.Find(classId)
        cls.className <- newName
        ctx.SaveChanges() |> ignore


    let changeMoreOf (classId : int) (moreOf : int) =
        use ctx = new AwareContext(Store.connectionString)
        let cls = ctx.Classes.Find(classId)
        cls.moreOf <- moreOf
        ctx.SaveChanges() |> ignore


    let loadByWellKnownTypeUsingContext (ctx : AwareContext) (classifierType : string) =
        query {
            for cls in ctx.Classifiers.Include(fun (c:Classifier) -> c.classes).Include(fun (c:Classifier) -> c.model) do
            where(cls.classifierType = classifierType)
            select cls
            exactlyOne
        }

    let needsReload (c : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let newC = ctx.Classifiers.Find(c.id)
        newC.classifierDefinitionVersion <> c.classifierDefinitionVersion

    let reLoad (oldClassifier : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        query {
            for cls in ctx.Classifiers.Include(fun (c:Classifier) -> c.classes).Include(fun (c:Classifier) -> c.model) do
            where(cls.id = oldClassifier.id)
            select cls
            exactlyOne
        }

    let loadByWellKnownType (classifierType : string) =
        use ctx = new AwareContext(Store.connectionString)
        loadByWellKnownTypeUsingContext ctx classifierType

    let loadCurrentCategoryCLassifierDefinition () =
        let classifier = loadByWellKnownType PrimitiveConstants.categoryClassifierType
        let loader = CategoryClassifierDefinitionLoader(classifier, needsReload, reLoad) :> IClassifierDefinitionLoader<CategoryClassifierDefinition>
        loader.ReadLatest

    let recategorizationPercentageDone () =  
        let classifier = loadByWellKnownType PrimitiveConstants.categoryClassifierType
        use ctx = new AwareContext(Store.connectionString)
        let totalAssignments = query {
                for sca in ctx.SampleClassAssignments do
                where (sca.classifierIdentifier = classifier.id)
                count
            }
        let incorrectlyVersionAssignments = query {
                for sca in ctx.SampleClassAssignments do
                where (sca.classifierIdentifier = classifier.id && sca.classifierDefinitionVersion <> classifier.classifierDefinitionVersion)
                count
            }
        if incorrectlyVersionAssignments <= totalAssignments && totalAssignments > 0 && incorrectlyVersionAssignments > 0 then
            let percent = int (100.0 * (float incorrectlyVersionAssignments) / (float totalAssignments)) 
            Some percent
        else
            None

    let saveWithModel (m : ClassifierModel) (settings : (string * int) list) =  
        use ctx = new AwareContext(Store.connectionString)
        let classifier = loadByWellKnownTypeUsingContext ctx PrimitiveConstants.categoryClassifierType
        let updatedVersion (name : string) =
            settings |> List.tryFind (fun (n,moreOf) -> name.Equals(n, System.StringComparison.CurrentCultureIgnoreCase))
        let removePhrases (c : ClassificationClass) =
            // Out with the old
            let old = (query {
                                for p in ctx.CategoryClassifierPhrases do
                                where (p.classificationClass.id = c.id)
                                select p
                            }) |> Seq.toList
            old
                |> Seq.iter (fun p -> ctx.CategoryClassifierPhrases.Remove(p) |> ignore)

        let updateIfRequired (c : ClassificationClass) =
            if c.idle || c.catchAll then
                ()
            else
                let updated = updatedVersion c.className
                if Option.isSome updated then
                    c.deleted <- false // ensure not marked as deleted
                else
                    c.deleted <- true
                    removePhrases c
                ctx.Classes.AddOrUpdate(c)
        let addNewClass x =
            let nm, moreOf = x
            let c = ClassificationClass(classifier = classifier, className = nm, catchAll = false, idle = false)
            ctx.Classes.Add(c) |> ignore
        Seq.iter updateIfRequired classifier.classes
        let existingClassNames = classifier.classes
                                        |> Seq.map (fun c -> c.className.ToUpperInvariant())
                                        |> Seq.toList
        let isNewClassName x = 
                let nm : string = fst x
                existingClassNames
                        |> List.exists (fun c -> nm.Equals(c, System.StringComparison.InvariantCultureIgnoreCase))
                        |> not
        let newClassSettings = settings |> List.filter isNewClassName
        let m = ctx.ClassifierModels.Attach(m)
        newClassSettings |> Seq.iter addNewClass
        classifier.classifierDefinitionVersion <- classifier.classifierDefinitionVersion + 1
        classifier.model <- m
        ctx.SaveChanges() |> ignore
        ()

    let saveNewCategorySettings (settings : (string * (string list)) list) =  
        // Read current classes and update
        //  for each current class if it is in the new list - delete and re add phrases -- mark as not deleted
        //      ignore the idle and catch all
        //  if a class is not in the new list - remove phrases and mark as deleted
        // 
        // for all classes that were not in the original list - create new classes
        // Update classifier version
        use ctx = new AwareContext(Store.connectionString)
        let classifier = loadByWellKnownTypeUsingContext ctx PrimitiveConstants.categoryClassifierType
        let updatedVersion (name : string) =
            settings |> List.tryFind (fun (n,_) -> name.Equals(n, System.StringComparison.CurrentCultureIgnoreCase))
        let updatePhrases (c : ClassificationClass) (phrases : string list) =
            // Out with the old
            let old = (query {
                                for p in ctx.CategoryClassifierPhrases do
                                where (p.classificationClass.id = c.id)
                                select p
                            }) |> Seq.toList
            old
                |> Seq.iter (fun p -> ctx.CategoryClassifierPhrases.Remove(p) |> ignore)
            // in with the new
            phrases
                |> List.map (fun p -> {id =0; phrase = p; orderBy = 0; classificationClass = c})
                |> List.iter (fun p -> ctx.CategoryClassifierPhrases.Add(p) |> ignore)
            
        
        let updateIfRequired (c : ClassificationClass) =
            if c.idle || c.catchAll then
                ()
            else
                let updated = updatedVersion c.className
                if Option.isSome updated then
                    let (m, phrases) = Option.get updated
                    c.deleted <- false // ensure not marked as deleted
                    updatePhrases c phrases
                else
                    c.deleted <- true
                    updatePhrases c List.empty
                ctx.Classes.AddOrUpdate(c)
        
        let addNewClass (n : string, ps : string list) =
            let c = ClassificationClass(classifier = classifier, className = n, catchAll = false, idle = false)
            ctx.Classes.Add(c) |> ignore
            updatePhrases c ps
        
        Seq.iter updateIfRequired classifier.classes
        let existingClassNames = classifier.classes
                                        |> Seq.map (fun c -> c.className.ToUpperInvariant())
                                        |> Seq.toList
        let isNewClassName (n : string) = existingClassNames
                                            |> List.exists (fun c -> n.Equals(c, System.StringComparison.InvariantCultureIgnoreCase))
                                            |> not
        let newClassSettings = settings |> List.filter (fun (n,_) -> isNewClassName n)
        newClassSettings |> Seq.iter addNewClass
        classifier.classifierDefinitionVersion <- classifier.classifierDefinitionVersion + 1
        ctx.SaveChanges() |> ignore

        

    let saveClassification (s : ActivitySample) (assignedClasses : ClassificationClass list) =
        if s.classes = null then
            s.classes <- System.Collections.Generic.List<SampleClassAssignment>()
            trace "Added new classes collection to sample: %d" s.id
        trace "Assigned %d classes to %d" (List.length assignedClasses) s.id
        use ctx = new AwareContext(Store.connectionString)

        let s = query {
                        for sample in ctx.ActivitySamples.Include(fun (x: ActivitySample) -> x.classes) do
                        where(sample.id = s.id)
                        exactlyOne
                }
        
        for c in assignedClasses do
            
            let existingAssignment = s.classes |> Seq.tryFind (fun a -> a.classifierIdentifier = c.classifier.id)
            let assignment = SampleClassAssignment()
                                
            assignment.classifierIdentifier <- c.classifier.id
            assignment.classifierDefinitionVersion <- c.classifier.classifierDefinitionVersion
            assignment.assignedClass <- ctx.Classes.Find(c.id)
            s.classes.Add assignment

            if Option.isSome existingAssignment then
                s.classes.Remove(Option.get existingAssignment) |> ignore
                ctx.SampleClassAssignments.Remove(Option.get existingAssignment) |> ignore
            
                
        ctx.SaveChanges() |> ignore


    let namedClassesInOrderOfUse (c : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let ids = ctx.Database.SqlQuery<int>("""SELECT cc.id
                                                    FROM ClassificationClasses cc
                                                    LEFT JOIN SampleClassAssignments sca
                                                        ON sca.classifierIdentifier = {0} AND sca.assignedClass_id = cc.id
                                                    WHERE
                                                        classifier_id = {0}
                                                        AND cc.idle = 0
                                                        AND cc.catchAll = 0
                                                        AND cc.deleted = 0
                                                    GROUP BY cc.id
                                                    ORDER BY COUNT(sca.id) DESC
                                                    """, c.id)
        let limitTo = if c.limitToMostUsed then
                        ids |> Seq.toList |> Seq.truncate c.classLimit |> Seq.toList
                      else
                        ids |> Seq.toList
                      
        (query {
            for cls in ctx.Classes.Include(fun (c: ClassificationClass) -> c.classifier) do
            where(limitTo.Contains(cls.id))
            select cls
        }) |> Seq.toList |> List.sortBy (fun c -> List.findIndex ((=) c.id) limitTo)


    let allClassifiers () =
        use ctx = new AwareContext(Store.connectionString)
        (query {
                for c in ctx.Classifiers.Include(fun (c : Classifier) -> c.classes).Include(fun (c : Classifier) -> c.model) do
                select c
                }) |> Seq.toList

    let allClassifierVersions () =
        use ctx = new AwareContext(Store.connectionString)
        (query {
                for c in ctx.Classifiers do
                select c.classifierDefinitionVersion
                }) |> Seq.toList

    let allClasses () =
        use ctx = new AwareContext(Store.connectionString)
        (query {
                for c in ctx.Classes.Include(fun (c : ClassificationClass) -> c.classifier) do
                select c
                }) |> Seq.toList
        


   
   
        

    let createClassifier (c : Classifier) =
        if c.classifierType = PrimitiveConstants.categoryClassifierType then
            Some (c.id, CategoryClassifier(CategoryClassifierDefinitionLoader(c, needsReload, reLoad)) :> IClassifier)
        elif c.classifierType = PrimitiveConstants.programClassifierType then
            Some (c.id, ProgramClassifier(ProgramClassifierDefinitionLoader(c, needsReload, reLoad)) :> IClassifier)
        else
            trace "Unknown classifier type: %s" c.classifierType
            None

    let loadAllClassifiers () =
        use ctx = new AwareContext(Store.connectionString)
        let classifiers = 
            (query {
                for c in ctx.Classifiers.Include(fun (c : Classifier) -> c.classes).Include(fun (c : Classifier) -> c.model) do
                select c
            })
        Seq.choose createClassifier classifiers |> Seq.toList

    let ensureCatchAllClassExists (existingClassifier : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let classifier = ctx.Classifiers.Attach(existingClassifier)
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = existingClassifier.id && c.catchAll = true)
            select c
            exactlyOneOrDefault
        }
        if (Operators.Unchecked.equals c null) then
            let c = ClassificationClass(classifier = existingClassifier, className = "Other", catchAll = true)
            ctx.Classes.Add(c) |> ignore
            ctx.SaveChanges() |> ignore
            c
        else
            c

    let ensureIdleClassExists (existingClassifier : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let classifier = ctx.Classifiers.Attach(existingClassifier)
        let c = query {
                        for c in ctx.Classes do
                            where (c.classifier.id = existingClassifier.id && c.idle = true)
                            select c
                            exactlyOneOrDefault
                            }
        if (Operators.Unchecked.equals c null) then
            let c = ClassificationClass(classifier = existingClassifier, className = "Idle", idle = true)
            ctx.Classes.Add(c) |> ignore
            ctx.SaveChanges() |> ignore
            c
        else
        c

    let ensureClassExists (existingClassifier : Classifier) (className : string) =
        use ctx = new AwareContext(Store.connectionString)
        let classifier = ctx.Classifiers.Attach(existingClassifier)
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = existingClassifier.id && c.className = className)
            select c
            exactlyOneOrDefault
        }
        if (Operators.Unchecked.equals c null) then
            let c = ClassificationClass(classifier = existingClassifier, className = className, catchAll = false)
            ctx.Classes.Add(c) |> ignore
            ctx.SaveChanges() |> ignore
            c
        else
            c

    let ensurePhraseExists (existingClass : ClassificationClass) (phrase : string) =
        use ctx = new AwareContext(Store.connectionString)
        let classifier = ctx.Classes.Attach(existingClass)
        let ps = (query {
                    for c in ctx.CategoryClassifierPhrases do
                    where (c.classificationClass.id = existingClass.id && c.phrase = phrase)
                    select c
                }) |> Seq.toList

        if (List.isEmpty ps) then
            let p = {id =0; phrase = phrase; orderBy = 0; classificationClass = existingClass}
            ctx.CategoryClassifierPhrases.Add(p) |> ignore
            ctx.SaveChanges() |> ignore
            p
        else
            List.head ps


    let ensureExistsByType (classifier : Classifier) =
        use ctx = new AwareContext(Store.connectionString)
        let c =
            query {
                for c in ctx.Classifiers do
                where (c.classifierType = classifier.classifierType)
                select c
                exactlyOneOrDefault
            }
        if (c = null) then
            let c = ctx.Classifiers.Add(classifier)
            ctx.SaveChanges() |> ignore
            c
        else
            c
        
    
