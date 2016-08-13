namespace BucklingSprings.Aware.Core.TimeSeriesPhantom

open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Utils

type Year = int
type Month = int
type DayOfMonth = int
type WeekOfYear = int
type Hour = int
type FromStartOfDay = System.TimeSpan
type Offset = System.TimeSpan

type TimePeriodI =
        | TimePeriodDaily of Year * Month * DayOfMonth * Offset
        | TimePeriodWeekly of Year * Month * DayOfMonth * Offset
        | TimePeriodDayOfWeek of System.DayOfWeek
        | TimePeriodHourOfDay of Hour
        | TimePeriodHourOfWork of Hour
        | TimePeriodTimeOfDay of FromStartOfDay
        | TimePeriodOpaque of string

type TimeSeriesPeriod<'a> = TimeSeriesPeriod of TimePeriodI

type TimeSeriesPeriodComplete = interface end
type TimeSeriesPeriodRegular = interface end

type TimeSeriesPeriodOpaque = interface end
type TimeSeriesPeriodTimeOfDay = interface end
type TimeSeriesPeriodDaily = 
    interface
        inherit TimeSeriesPeriodRegular
    end


type TimeSeriesPeriodWeekly =
    interface 
        inherit TimeSeriesPeriodRegular
    end

type TimeSeriesPeriodDayOfWeek = 
    interface 
        inherit TimeSeriesPeriodComplete
    end

type TimeSeriesPeriodHourOfDay = 
    interface 
        inherit TimeSeriesPeriodComplete
    end

type TimeSeriesPeriodHourOfWork = 
    interface 
        inherit TimeSeriesPeriodComplete
    end

type TimeSeriesOrderedByPeriod = interface end
type TimeSeriesOrderUnknown = interface end
type TimeSeriesOrderedByPeriodDesc = interface end

type TimeSeriesRegular = interface end
type TimeSeriesRegularityUnknown = interface end

type TimeSeriesComplete = interface end
type TimeSeriesCompletenessUnknown = interface end

type TimeSeriesPhantom<'s, 'r, 'c, 'p, 't> = TimeSeriesPhantom of (TimeSeriesPeriod<'p> * 't) list

module TimeSeriesPeriods =

    let mkDaily ( d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodDaily> =
        TimeSeriesPeriod(TimePeriodDaily(d.Year, d.Month, d.Day, d.Offset))
    
    let mkDailyRegular ( d : System.DateTimeOffset) : TimeSeriesPeriod<#TimeSeriesPeriodRegular> =
        TimeSeriesPeriod(TimePeriodDaily(d.Year, d.Month, d.Day, d.Offset))

    let mkWeekly ( d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodWeekly> =
        let sow = d.StartOfWeek
        TimeSeriesPeriod(TimePeriodWeekly(sow.Year, sow.Month, sow.Day,sow.Offset))

    let mkWeeklyRegular ( d : System.DateTimeOffset) : TimeSeriesPeriod<#TimeSeriesPeriodRegular> =
        TimeSeriesPeriod(TimePeriodWeekly(d.Year, d.Month, d.Day, d.Offset))

    let mkDayOfWeek (d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodDayOfWeek>= 
        TimeSeriesPeriod(TimePeriodDayOfWeek(d.DayOfWeek))

    let mkHourOfDay (d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodHourOfDay>= 
        TimeSeriesPeriod(TimePeriodHourOfDay(d.Hour))

    let mkHourOfWork (d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodHourOfWork>= 
        TimeSeriesPeriod(TimePeriodHourOfWork(d.Hour))

    let mkTimeOfDay (d : System.DateTimeOffset) : TimeSeriesPeriod<TimeSeriesPeriodTimeOfDay>= 
        let ts = d.Subtract(d.StartOfDay)
        TimeSeriesPeriod(TimePeriodTimeOfDay(ts))

    let mkOpaque ( s : string) : TimeSeriesPeriod<TimeSeriesPeriodOpaque> =
        TimeSeriesPeriod(TimePeriodOpaque(s))

    let day (p : TimeSeriesPeriod<TimeSeriesPeriodDaily>) : System.DateTimeOffset =
        match p with 
        | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)) -> new System.DateTimeOffset(y,m,d,0,0,0,o)
        | _ -> failwith "Type Mismatch"
    
    let hourOfDay (p : TimeSeriesPeriod<TimeSeriesPeriodHourOfDay>) : int =
        match p with 
        | TimeSeriesPeriod(TimePeriodHourOfDay(h)) -> h
        | _ -> failwith "Type Mismatch"

    let dayOfWeek (p : TimeSeriesPeriod<TimeSeriesPeriodDayOfWeek>) : System.DayOfWeek =
        match p with 
        | TimeSeriesPeriod(TimePeriodDayOfWeek(dow)) -> dow
        | _ -> failwith "Type Mismatch"

    let toDebugString (p : TimeSeriesPeriod<'a>) : string =
        match p with
        | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)) -> sprintf "TimePeriodDaily(Year=%d,Month=%d,Day=%d,Offset=%A)" y m d o
        | TimeSeriesPeriod(TimePeriodWeekly(y,m,d,o)) -> sprintf "TimePeriodWeekly(Year=%d,Month=%d,Day=%d,Offset=%A)" y m d o
        | TimeSeriesPeriod(TimePeriodDayOfWeek(dow)) -> sprintf "TimePeriodDayOfWeek(DayOfWeek=%A)" dow
        | TimeSeriesPeriod(TimePeriodHourOfDay(h)) -> sprintf "TimePeriodHourOfDay(Hour=%d)" h
        | TimeSeriesPeriod(TimePeriodHourOfWork(h)) -> sprintf "TimePeriodHourOfWork(Hour=%d)" h
        | TimeSeriesPeriod(TimePeriodTimeOfDay(ts)) -> sprintf "TimePeriodTimeOfDay(From Start Of Day=%s)" (ts.ToString())
        | TimeSeriesPeriod(TimePeriodOpaque(s)) -> sprintf "TimePeriodOpaque(label=%s)" s

    let humanize (p : TimeSeriesPeriod<'a>) : string =
        match p with
        | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)) -> let dt = System.DateTimeOffset(y,m,d,0,0,0,o) in Humanize.dateTimeOffset dt
        | TimeSeriesPeriod(TimePeriodWeekly(y,m,d,o)) -> let dt = System.DateTimeOffset(y,m,d,0,0,0,o)
                                                         let endDt = dt.AddDays(7.0)
                                                         let endDtOrNow = if endDt > System.DateTimeOffset.Now then System.DateTimeOffset.Now else endDt
                                                         in sprintf "%s - %s"  (Humanize.dateTimeOffset dt) (Humanize.dateTimeOffset endDtOrNow)
        | TimeSeriesPeriod(TimePeriodDayOfWeek(dow)) -> Humanize.dayOfWeek (int dow)
        | TimeSeriesPeriod(TimePeriodHourOfDay(h)) -> Humanize.hoursFromStartOfDayToTimeFormat h
        | TimeSeriesPeriod(TimePeriodHourOfWork(h)) -> Humanize.hoursFromStartOfDayToOrdinal h
        | TimeSeriesPeriod(TimePeriodTimeOfDay(ts)) -> Humanize.minutesFromStartOfDayF ts.TotalMinutes
        | TimeSeriesPeriod(TimePeriodOpaque(s)) -> s

    let areSame (p : TimeSeriesPeriod<'a> * TimeSeriesPeriod<'a>) : bool =
        match p with
        | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)), TimeSeriesPeriod(TimePeriodDaily(y1,m1,d1,o1)) -> y = y1 && m = m1 && d = d1
        | TimeSeriesPeriod(TimePeriodWeekly(y,m,d,o)),TimeSeriesPeriod(TimePeriodWeekly(y1,m1,d1,o1)) -> y = y1 && m = m1 && d = d1
        | TimeSeriesPeriod(TimePeriodDayOfWeek(dow)),TimeSeriesPeriod(TimePeriodDayOfWeek(dow1)) -> dow = dow1
        | TimeSeriesPeriod(TimePeriodHourOfDay(h)), TimeSeriesPeriod(TimePeriodHourOfDay(h1)) -> h = h1
        | TimeSeriesPeriod(TimePeriodHourOfWork(h)), TimeSeriesPeriod(TimePeriodHourOfWork(h1)) -> h = h1
        | TimeSeriesPeriod(TimePeriodTimeOfDay(ts)), TimeSeriesPeriod(TimePeriodTimeOfDay(ts1)) -> (int ts.TotalSeconds) = (int ts1.TotalSeconds)
        | _ -> false

    let allDaysOfWeek : TimeSeriesPeriod<TimeSeriesPeriodDayOfWeek> list =
        let someSunday = System.DateTimeOffset.Now.StartOfWeek
        [ for i in 0 .. 6 -> someSunday.AddDays(float i) |> mkDayOfWeek ]

    let allHoursOfDays : TimeSeriesPeriod<TimeSeriesPeriodHourOfDay> list =
        let someDay = System.DateTimeOffset.Now.StartOfDay
        [ for i in 0 .. 23 -> someDay.AddHours(float i) |> mkHourOfDay ]

    let allHoursOfWork : TimeSeriesPeriod<TimeSeriesPeriodHourOfWork> list =
        let someDay = System.DateTimeOffset.Now.StartOfDay
        [ for i in 0 .. 23 -> someDay.AddHours(float i) |> mkHourOfWork ]

    let all<'p when 'p :> TimeSeriesPeriodComplete> : TimeSeriesPeriod<'p> list =
        let r : Lazy<'p> = lazy ( failwith "Undefined")
        match (box r) with
        | :? Lazy<TimeSeriesPeriodDayOfWeek> -> allDaysOfWeek |> Seq.cast |> Seq.toList
        | :? Lazy<TimeSeriesPeriodHourOfDay> -> allHoursOfDays |> Seq.cast |> Seq.toList
        | :? Lazy<TimeSeriesPeriodHourOfWork> -> allHoursOfWork |> Seq.cast |> Seq.toList
        | _ -> failwith "Type Mismatch"

    let addN<'p when 'p :> TimeSeriesPeriodRegular> (x : TimeSeriesPeriod<'p>) (n : int)  : TimeSeriesPeriod<'p> = 
        match x with
        | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)) -> 
            let dt = new System.DateTimeOffset(y,m,d,0,0,0,o)
            let nextDt = dt.AddDays(float n)
            mkDailyRegular nextDt
        | TimeSeriesPeriod(TimePeriodWeekly(y,m,d,o)) -> 
            let dt = new System.DateTimeOffset(y,m,d,0,0,0,o)
            let nextDt = dt.AddDays(float (n * 7))
            mkWeeklyRegular nextDt

        | _ -> failwith "Type Mismatch"

    let periodsEndingAt<'p when 'p :> TimeSeriesPeriodRegular> (count : int) (endingPeriod : TimeSeriesPeriod<'p>) : TimeSeriesPeriod<'p> list = 
        let start = addN endingPeriod (- count + 1)
        Seq.init count (addN start) |> Seq.toList

        

module TimeSeries =

    let mkSeries (periodProjection : 'a -> TimeSeriesPeriod<'p>) (mapping : 'a list -> 'b) (data : 'a list) : TimeSeriesPhantom<TimeSeriesOrderUnknown, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'b> =
        TimeSeriesPhantom(data |> Seq.groupBy periodProjection |> Seq.map (fun (p, d) -> (p, Seq.toList d |> Seq.toList |> mapping)) |> Seq.toList)

    let mkEmpty<'p, 'a> : TimeSeriesPhantom<TimeSeriesOrderUnknown, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'a> =
        TimeSeriesPhantom([])

    let length (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 't>) : int = List.length xs

    let sort (TimeSeriesPhantom(xs) : TimeSeriesPhantom<TimeSeriesOrderUnknown, 'r, 'c, 'p, 't>) : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, 'r, 'c, 'p, 't> = 
        TimeSeriesPhantom(List.sortBy fst xs)

    let reverse (TimeSeriesPhantom(xs) : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, 'r, 'c, 'p, 't>) : TimeSeriesPhantom<TimeSeriesOrderedByPeriodDesc, 'r, 'c, 'p, 't> = 
        TimeSeriesPhantom(List.rev xs)

    let trace (tracer : 't -> unit) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 't>) : Unit =
        let trace' (p,d) = 
            trace "\t%s:" (TimeSeriesPeriods.toDebugString p)
            tracer d
        xs |> List.iter trace'


    let fmap (mapping : 'a -> 'b) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a>) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'b> =
        TimeSeriesPhantom(xs |> List.map (fun (p, data)  -> (p, mapping data)))

    let extractPeriods (mapping : TimeSeriesPeriod<'p> -> 'b) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a>) : 'b list =
        xs |> List.map (fst >> mapping)

    let extractData (mapping : 'a -> 'b) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a>) : 'b list =
        xs |> List.map (snd >> mapping)

    let extractDatai (mapping : int -> 'a -> 'b) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a>) : 'b list =
        xs |> List.mapi (fun i ->  (snd >> mapping i))

    let extractDataAndPeriodi (mapping : int -> TimeSeriesPeriod<'p> -> 'a -> 'b) (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a>) : 'b list =
        xs |> List.mapi (fun i (p,d) ->  mapping i p d )

    let complete<'s, 'r, 'a, 'p when 'p :> TimeSeriesPeriodComplete> 
            (empty : TimeSeriesPeriod<'p> -> 'a)
            (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, TimeSeriesCompletenessUnknown, 'p, 'a>) : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, 'p, 'a> =
                let periodsWithValues = Map(xs)
                let allPeriods = TimeSeriesPeriods.all<'p>
                let createEmptyIfRequired p = if periodsWithValues.ContainsKey p then periodsWithValues.Item p else (empty p)
                let pairs = allPeriods |> List.map (fun p -> (p, createEmptyIfRequired p))
                TimeSeriesPhantom(pairs)

    let opaque<'s, 'r, 'c, 'p, 't> (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, 'r, 'c, 'p, 't>) : TimeSeriesPhantom<'s, 'r, 'c, TimeSeriesPeriodOpaque, 't> =
        TimeSeriesPhantom(xs |> List.map (fun (p,d) -> (TimeSeriesPeriods.mkOpaque (TimeSeriesPeriods.humanize p), d)))

    let regular<'s, 'r, 'a, 'p when 'p :> TimeSeriesPeriodRegular>
        (empty : TimeSeriesPeriod<'p> -> 'a)
        (count : int)
        (endingOn : TimeSeriesPeriod<'p>)
        (TimeSeriesPhantom(xs) : TimeSeriesPhantom<'s, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'a>) : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesCompletenessUnknown, 'p, 'a> =
            let periods = TimeSeriesPeriods.periodsEndingAt count endingOn
            let dataForPeriod period = 
                let dO = xs |> List.tryPick (fun (p,x) -> if TimeSeriesPeriods.areSame(p, period) then Some x else None)
                if Option.isNone dO then empty period else Option.get dO
            TimeSeriesPhantom(periods |> List.map (fun p -> (p, dataForPeriod p)))
            
[<Sealed>]
type TimeSeriesPeriodConversions =
    static member toDay (d : TimeSeriesPeriod<TimeSeriesPeriodDaily>) : System.DateTimeOffset =
                match d with
                    | TimeSeriesPeriod(TimePeriodDaily(y,m,d,o)) -> new System.DateTimeOffset(y, m, d, 0, 0, 0, o)
                    | _ -> failwith "Type Mismatch"

    static member toDay (d : TimeSeriesPeriod<TimeSeriesPeriodWeekly>) : System.DateTimeOffset =
                match d with
                    | TimeSeriesPeriod(TimePeriodWeekly(y,m,d,o)) -> new System.DateTimeOffset(y, m, d, 0, 0, 0, o)
                    | _ -> failwith "Type Mismatch"

    static member toTimeSpan (d : TimeSeriesPeriod<TimeSeriesPeriodTimeOfDay>) : System.TimeSpan =
        match d with
                    | TimeSeriesPeriod(TimePeriodTimeOfDay(ts)) -> ts
                    | _ -> failwith "Type Mismatch"