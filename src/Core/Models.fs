namespace BucklingSprings.Aware.Core.Models

open System.Collections.Generic

module EqualityHelper =

     let equalsOn f x (yobj:obj) = 
        match yobj with
        | :? 'T as y -> (f x = f y)
        | _ -> false


type SampleTime = System.DateTimeOffset
type ProcessName = string
type WindowTitle = string

type MoreOf =
        | MoreOf
        | LessOf
        | Neutral


[<AllowNullLiteral()>]
type SearchResult() =
    member val keyboardActivity = 0 with get, set
    member val seconds = 0 with get, set


[<AllowNullLiteral()>]
type DailyTotal() =
    member val sampleDate = Operators.Unchecked.defaultof<System.DateTime> with get, set
    member val keyboardActivity = 0 with get, set
    member val seconds = 0 with get, set

[<AllowNullLiteral()>]
type HourlyTotal() =
    member val sampleDate = Operators.Unchecked.defaultof<System.DateTime> with get, set
    member val sampleHour = 0 with get, set
    member val keyboardActivity = 0 with get, set
    member val seconds = 0 with get, set

type DailyPerformance =
    {
        words : int
        minutes : int
    }


[<CLIMutable()>]
type InputActivity =
    {
        keyboardActivity : int
        mouseActivity : int
    }
    member x.IsNotIdle = x.keyboardActivity > 0 || x.mouseActivity > 0
    static member (+) (x : InputActivity, y : InputActivity) =
        { keyboardActivity = x.keyboardActivity + y.keyboardActivity; mouseActivity = x.mouseActivity + y.mouseActivity }
    static member Zero = { keyboardActivity = 0; mouseActivity = 0}
    

type UnsavedSample = {
    timeAndDate : SampleTime
    inputActivity : InputActivity
    processName : ProcessName
    windowTitle : WindowTitle}

    
[<AllowNullLiteral()>]
type Process() =
    member val id = 0 with get, set
    member val processName =  Operators.Unchecked.defaultof<string> with get, set


[<AllowNullLiteral()>]
type ClassifierModel() =
    member val id = 0 with get, set
    member val createdOn = Operators.Unchecked.defaultof<System.DateTimeOffset> with get, set
    


type [<AllowNullLiteral()>] Classifier() =
    member val id = 0 with get, set
    member val classifierName = Operators.Unchecked.defaultof<string> with get, set
    member val classifierType = Operators.Unchecked.defaultof<string> with get, set
    member val model = Operators.Unchecked.defaultof<ClassifierModel> with get, set
    member val limitToMostUsed = true with get, set
    member val classifierDefinitionVersion = 0 with get, set
    member val classLimit = 6 with get, set
    member val classes = Operators.Unchecked.defaultof<ClassificationClass ICollection> with get, set
    override x.Equals y = EqualityHelper.equalsOn (fun (c: Classifier) -> c.id) x y
    override x.GetHashCode() = x.id
and [<AllowNullLiteral()>] ClassificationClass() =
    member val id = 0 with get, set
    member val classifier = Operators.Unchecked.defaultof<Classifier> with get, set
    member val className =  Operators.Unchecked.defaultof<string> with get, set
    member val catchAll =  false with get, set
    member val idle =  false with get, set
    member val deleted = false with get, set
    member val moreOf = 0 with get, set
    override x.Equals y = EqualityHelper.equalsOn (fun (c: ClassificationClass) -> c.id) x y
    override x.GetHashCode() = x.id

[<CLIMutable()>]
[<NoComparison()>]
type CategoryClassifierPhrase  = {
    id : int
    phrase : string
    orderBy : int
    classificationClass : ClassificationClass}

type [<AllowNullLiteral()>] ActivityWindowDetail() =
    member val id = 0 with get, set
    member val windowText =  Operators.Unchecked.defaultof<string> with get, set
    member val processInformation = Operators.Unchecked.defaultof<Process> with get, set

    

type ActivitySample() =
    member val id = 0 with get, set
    member val sampleStartTimeAndDate = Operators.Unchecked.defaultof<SampleTime> with get, set
    member val sampleEndTimeAndDate = Operators.Unchecked.defaultof<SampleTime> with get, set
    member val inputActivity = Operators.Unchecked.defaultof<InputActivity> with get, set
    member val activityWindowDetail = Operators.Unchecked.defaultof<ActivityWindowDetail> with get, set
    member val classes = Operators.Unchecked.defaultof<SampleClassAssignment ICollection> with get, set
and SampleClassAssignment() =
    member val id = 0 with get, set
    member val assignedClass = Operators.Unchecked.defaultof<ClassificationClass> with get, set
    member val classifierDefinitionVersion = 0 with get, set
    member val classifierIdentifier = 0 with get, set
    

[<CLIMutable()>]
[<NoComparison()>]
type StoredSummary = {
    id : int
    summaryType: string
    summaryTimeAndDate : System.DateTimeOffset
    summary : string
    summaryClass : ClassificationClass
    summaryClassifierDefinitionVersion : System.Nullable<int>
    summaryClassifierIdentifier : System.Nullable<int>}

[<CLIMutable()>]
[<NoComparison()>]
type StoredGoal = {
        id : int
        value : int
        comparison: string
        period : string
        target : string
        goalClass : ClassificationClass
        startTime : System.DateTimeOffset
        endTime : System.DateTimeOffset
    }

[<CLIMutable()>]
[<NoComparison()>]
type Configuration = {
    id : int
    name : string
    value : string
}

[<CLIMutable()>]
[<NoComparison()>]
type ClassifierModelExample = {
    id : int
    model : ClassifierModel
    windowDetail : ActivityWindowDetail
    trained : int
}