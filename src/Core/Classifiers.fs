namespace BucklingSprings.Aware.Core.Classifiers

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Diagnostics

type IClassifierDefinition =
    abstract DefinitionVersion : int
    abstract ClassifierId : int
    abstract IdleClass : ClassificationClass

type IClassifier =
    abstract Classify : ActivitySample -> ClassificationClass

type IClassifierDefinitionLoader<'a when 'a :>  IClassifierDefinition> =
    abstract ReadLatest : 'a
    abstract SaveNewClass : ClassificationClass -> ClassificationClass

type SimpleClassifierDefinition(classifier : Classifier, idleClass : ClassificationClass) =
    interface IClassifierDefinition with
        member x.DefinitionVersion = classifier.classifierDefinitionVersion
        member x.ClassifierId = classifier.id
        member x.IdleClass = idleClass

type ProgramClassifierDefinition(classifier : Classifier, idleClass : ClassificationClass, programClasses : Map<string, ClassificationClass>) =
   inherit SimpleClassifierDefinition(classifier, idleClass)
   member x.ProgramClassByProgramName programName = Map.tryFind programName programClasses
   member x.NewProgramClassByProgramName programName = ClassificationClass(classifier = classifier, className = programName)

type CategoryClassifierDefinition(classifier : Classifier, idleClass : ClassificationClass, catchAllClass : ClassificationClass, namedClasses : ClassificationClass list, model) =
   inherit SimpleClassifierDefinition(classifier, idleClass)
   member x.CatchAllClass = catchAllClass
   member x.Classes = namedClasses
   member x.Model : BucklingSprings.Classification.ModelState<string, string> option = model


type ProgramClassifier(loader : IClassifierDefinitionLoader<ProgramClassifierDefinition>) =
    let classify (sample : ActivitySample) = 
        let definition = loader.ReadLatest
        if sample.inputActivity.IsNotIdle then
            let prcNm = sample.activityWindowDetail.processInformation.processName
            let existingProgramClass = definition.ProgramClassByProgramName prcNm
            if Option.isSome existingProgramClass then
                Option.get existingProgramClass
            else
                trace "Saving new class: %s" prcNm
                loader.SaveNewClass (definition.NewProgramClassByProgramName prcNm)
        else
            let simpleClassDef = definition :> IClassifierDefinition
            simpleClassDef.IdleClass

    interface IClassifier with
        member x.Classify input = classify input

type CategoryClassifier(loader : IClassifierDefinitionLoader<CategoryClassifierDefinition>) =
    let classify (sample : ActivitySample) =
        let definition = loader.ReadLatest
        if sample.inputActivity.IsNotIdle then
            if Option.isNone definition.Model then
                definition.CatchAllClass
            else
                let m = Option.get definition.Model
                let features = Set.ofList (Phrase.extractWords sample.activityWindowDetail)
                let preferOther (c,_) = if c = definition.CatchAllClass.className then 1 else 0
                let predictedClassName = BucklingSprings.Classification.Classifier.predictMode features preferOther m
                let c =  definition.Classes |> Seq.tryFind (fun c -> c.className = predictedClassName)
                if Option.isNone c then
                    definition.CatchAllClass
                else
                     Option.get c
        else
            let simpleClassDef = definition :> IClassifierDefinition
            simpleClassDef.IdleClass
    interface IClassifier with
        member x.Classify input = classify input

