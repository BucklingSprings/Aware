namespace BucklingSprings.Aware.Core.TimeSeriesPhantom

type TimeSeriesPeriod<'a>

type TimeSeriesPeriodComplete = interface end
type TimeSeriesPeriodRegular = interface end

type TimeSeriesPeriodTimeOfDay = interface end
type TimeSeriesPeriodOpaque = interface end

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

type TimeSeriesPhantom<'s, 'r, 'c, 'p, 't>

module TimeSeriesPeriods =

    val mkDaily : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodDaily>
    val mkWeekly : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodWeekly>
    val mkDayOfWeek : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodDayOfWeek>
    val mkHourOfDay : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodHourOfDay>
    val mkHourOfWork : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodHourOfWork>
    val mkTimeOfDay : System.DateTimeOffset -> TimeSeriesPeriod<TimeSeriesPeriodTimeOfDay>

    val hourOfDay : TimeSeriesPeriod<TimeSeriesPeriodHourOfDay> -> int
    val dayOfWeek : TimeSeriesPeriod<TimeSeriesPeriodDayOfWeek> -> System.DayOfWeek

    val humanize : TimeSeriesPeriod<'a> -> string
    val toDebugString : TimeSeriesPeriod<'a> -> string

module TimeSeries =

    val mkSeries : ('a -> TimeSeriesPeriod<'p>) -> ('a list -> 'b) -> 'a list -> TimeSeriesPhantom<TimeSeriesOrderUnknown, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'b>
    val mkEmpty<'p, 'a> : TimeSeriesPhantom<TimeSeriesOrderUnknown, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'a>
    val length : TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> int
    val sort : TimeSeriesPhantom<TimeSeriesOrderUnknown, 'r, 'c, 'p, 'a> -> TimeSeriesPhantom<TimeSeriesOrderedByPeriod, 'r, 'c, 'p, 'a>
    val reverse : TimeSeriesPhantom<TimeSeriesOrderedByPeriod, 'r, 'c, 'p, 'a> -> TimeSeriesPhantom<TimeSeriesOrderedByPeriodDesc, 'r, 'c, 'p, 'a>
    val trace : ('a -> Unit) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> Unit

    
    val fmap :  ('a -> 'b) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'b>
    val extractPeriods  : (TimeSeriesPeriod<'p> -> 'b) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> 'b list
    val extractData  : ('a -> 'b) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> 'b list
    val extractDatai  : (int -> 'a -> 'b) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> 'b list
    val extractDataAndPeriodi  : (int -> TimeSeriesPeriod<'p> -> 'a -> 'b) -> TimeSeriesPhantom<'s, 'r, 'c, 'p, 'a> -> 'b list

    val complete<'s, 'r, 'a, 'p when 'p :> TimeSeriesPeriodComplete> :
            (TimeSeriesPeriod<'p> -> 'a)
            -> (TimeSeriesPhantom<'s, 'r, TimeSeriesCompletenessUnknown, 'p, 'a>) 
            -> TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesComplete, 'p, 'a>

    val regular<'s, 'r, 'a, 'p when 'p :> TimeSeriesPeriodRegular> :
            (TimeSeriesPeriod<'p> -> 'a)
            -> int
            -> TimeSeriesPeriod<'p>
            -> (TimeSeriesPhantom<'s, TimeSeriesRegularityUnknown, TimeSeriesCompletenessUnknown, 'p, 'a>) 
            -> TimeSeriesPhantom<TimeSeriesOrderedByPeriod, TimeSeriesRegular, TimeSeriesCompletenessUnknown, 'p, 'a>

    val opaque<'s, 'r, 'c, 'p, 't> : TimeSeriesPhantom<'s, 'r, 'c, 'p, 't> -> TimeSeriesPhantom<'s, 'r, 'c, TimeSeriesPeriodOpaque, 't>
            
    

[<Sealed>]
type TimeSeriesPeriodConversions =
    static member toDay : TimeSeriesPeriod<TimeSeriesPeriodDaily> -> System.DateTimeOffset
    static member toDay : TimeSeriesPeriod<TimeSeriesPeriodWeekly> -> System.DateTimeOffset
    static member toTimeSpan : TimeSeriesPeriod<TimeSeriesPeriodTimeOfDay> -> System.TimeSpan