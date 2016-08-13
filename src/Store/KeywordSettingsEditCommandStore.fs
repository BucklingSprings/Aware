namespace BucklingSprings.Aware.Store.Settings

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Settings
open BucklingSprings.Aware.Entitities
open BucklingSprings.Aware.Store

module KeywordSettingsEditCommandStore =

    let renameClass (ctx : AwareContext) (classifier : Classifier) oldName newName =
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = classifier.id && c.className = oldName)
            select c
            exactlyOne
        }
        c.className <- newName

    let addClass (ctx : AwareContext) (classifier : Classifier) name =
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = classifier.id && c.className = name)
            select c
            exactlyOneOrDefault
        }
        if (Operators.Unchecked.equals c null) then
            let c = ClassificationClass(classifier = classifier, className = name, catchAll = false)
            ctx.Classes.Add(c) |> ignore
        else
            c.deleted <- false

    let deleteClass (ctx : AwareContext) (classifier : Classifier) name =
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = classifier.id && c.className = name)
            select c
            exactlyOne
        }
        c.deleted <- true
        let phrases = query {
            for p in ctx.CategoryClassifierPhrases do
            where (p.classificationClass.id = c.id)
        }
        phrases |> Seq.iter (fun p -> ctx.CategoryClassifierPhrases.Remove(p) |> ignore)

    let deletePhrase (ctx : AwareContext) (classifier : Classifier) className phrase =
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = classifier.id && c.className = className)
            select c
            exactlyOne
        }
        let phrases = query {
            for p in ctx.CategoryClassifierPhrases do
            where (p.classificationClass.id = c.id)
        }
        phrases |> Seq.iter (fun p -> ctx.CategoryClassifierPhrases.Remove(p) |> ignore)

    let addPhrase (ctx : AwareContext) (classifier : Classifier) className phrase =
        let c = query {
            for c in ctx.Classes do
            where (c.classifier.id = classifier.id && c.className = className)
            select c
            exactlyOne
        }
        let p = {id =0; phrase = phrase; orderBy = 0; classificationClass = c}
        ctx.CategoryClassifierPhrases.Add(p) |> ignore


    let saveCommand (c : KeywordSettingsEditCommand) =
        use ctx = new AwareContext(Store.connectionString)
        let classifier =
            query {
                for c in ctx.Classifiers do
                where (c.classifierType = PrimitiveConstants.categoryClassifierType)
                select c
                exactlyOne
            }
        if KeywordSettingsEditCommands.requiresVersionChange c then
            classifier.classifierDefinitionVersion <- classifier.classifierDefinitionVersion + 1
        match c with
            | RenameClass(oldName, newName) -> renameClass ctx classifier oldName newName
            | DeleteClass name -> deleteClass ctx classifier name
            | AddClass name -> addClass ctx classifier name
            | DeletePhrase(className,phrase) -> deletePhrase ctx classifier className phrase
            | AddPhrase (className,phrase) -> addPhrase ctx classifier className phrase
        ctx.SaveChanges() |> ignore
        

    let save (commands : KeywordSettingsEditCommand list) =
        commands |> List.iter saveCommand
