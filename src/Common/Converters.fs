namespace BucklingSprings.Aware.Data

open System
open System.Windows.Data

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Utils

[<AbstractClass()>]
type CastingOneWayConverterBase<'a, 'b>(unset : 'b) =
    abstract member Conv : 'a -> 'b
    interface IValueConverter with
        member x.Convert(value,_,_,_) =
            let converted = match value with
                                | :? 'a as v ->  x.Conv v
                                | _ -> unset
            upcast converted
        member x.ConvertBack(_,_,_,_) = failwith "One Way Conversion"

[<AbstractClass()>]
type CastingTwoWayConverterBase<'a, 'b>(unsetA : 'a, unsetB : 'b) =
    abstract member Conv : 'a -> 'b
    abstract member ConvBack : 'b -> 'a
    interface IValueConverter with
        member x.Convert(value,_,_,_) =
            let converted = match value with
                                | :? 'a as v ->  x.Conv v
                                | _ -> unsetB
            upcast converted
        member x.ConvertBack(value,_,_,_) = 
            let converted = match value with
                                | :? 'b as v ->  x.ConvBack v
                                | _ -> unsetA
            upcast converted


[<AbstractClass()>]
type CastingOneWayMultiConverterBase<'a, 'b, 'c>(unset : 'c) =
    abstract member Conv : ('a * 'b) -> 'c
    interface IMultiValueConverter with
        member x.ConvertBack(_,_,_,_) = failwith "One Way Conversion"
        member x.Convert(values,_,_,_) =
            let converted = try
                                let a : 'a = downcast values.[0]
                                let b : 'b = downcast values.[1]
                                x.Conv(a,b)
                            with
                                | _ ->  unset
            converted :> obj

[<AbstractClass()>]
type CastingOneWayMultiConverterBase<'a, 'b, 'c, 'd>(unset : 'd) =
    abstract member Conv : ('a * 'b * 'c) -> 'd
    interface IMultiValueConverter with
        member x.ConvertBack(_,_,_,_) = failwith "One Way Conversion"
        member x.Convert(values,_,_,_) =
            let converted = try
                                let a : 'a = downcast values.[0]
                                let b : 'b = downcast values.[1]
                                let c : 'c = downcast values.[2]
                                x.Conv(a,b,c)
                            with
                                | _ ->  unset
            converted :> obj

type ProbabilityToOpacityConverter() =
    inherit CastingOneWayConverterBase<float, float>(1.0)
    override x.Conv p = if p < 0.25 then 0.25 else p

type StringToUpperCaseConverter() =
    inherit CastingOneWayConverterBase<string, string>(null)
    override x.Conv s = s.ToUpperInvariant()

type WordCountHumanizer() =
    inherit CastingOneWayConverterBase<int, string>("")
    override x.Conv t = System.String.Format("{0:n0}", t)

type TimeOfDayHumanizer() =
    inherit CastingOneWayConverterBase<int, string>("")
    override x.Conv t = Humanize.minutesFromStartOfDayAsTime t

type WordsPerMinuteHumanizer() =
    inherit CastingOneWayConverterBase<int, string>("")
    override x.Conv t = System.String.Format("{0:n0}", t)

type MinuteCountHumanizer() =
    inherit CastingOneWayConverterBase<int, string>("")
    override x.Conv t = 
        System.String.Format("{0}", t)

type DayOfWeekHumanizer() =
    inherit CastingOneWayConverterBase<DayOfWeek, string>("")
    override x.Conv t = Humanize.dayOfWeek (int t)

type HourOfDayHumanizer() =
    inherit CastingOneWayConverterBase<int, string>("")
    override x.Conv t = Humanize.hoursFromStartOfDayAsRange t

type DateTimeOffsetToTimeHumanizer() =
    inherit CastingOneWayConverterBase<DateTimeOffset, string>("")
    override x.Conv t = Humanize.time t

