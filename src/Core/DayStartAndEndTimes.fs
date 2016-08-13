#nowarn "52"

namespace BucklingSprings.Aware.Core.DayStartAndEndTimes

open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

[<RequireQualifiedAccess()>]
type TimeOfDay =
    FromStartOfDay of int
        static member (+)  (x : TimeOfDay, y : TimeOfDay) =
            let (TimeOfDay.FromStartOfDay x') = x
            let (TimeOfDay.FromStartOfDay y') = y
            TimeOfDay.FromStartOfDay (x' + y')
        static member Zero = TimeOfDay.FromStartOfDay 0


[<RequireQualifiedAccess()>]
type DayLength =
    {
        startTime : TimeOfDay
        endTime : TimeOfDay
    }
    static member (+) (x : DayLength, y : DayLength) =
        { startTime = x.startTime + y.startTime ; endTime = x.endTime + y.endTime }
    static member Zero = { startTime = TimeOfDay.Zero; endTime = TimeOfDay.Zero}


[<RequireQualifiedAccess()>]
type TimeOfDayLengthKind =
    | DayStartTime 
    | DayEndTime

module StartAndEndTime =

    let toMinutesFromStartOfDay (TimeOfDay.FromStartOfDay i) = i

    let fromDtTime (d : System.DateTimeOffset) = 
        let m = int (d.Subtract(d.StartOfDay).TotalMinutes)
        TimeOfDay.FromStartOfDay m

    let max x y = 
        let (TimeOfDay.FromStartOfDay x') = x
        let (TimeOfDay.FromStartOfDay y') = y
        TimeOfDay.FromStartOfDay (max x' y')

    let min x y = 
        let (TimeOfDay.FromStartOfDay x') = x
        let (TimeOfDay.FromStartOfDay y') = y
        TimeOfDay.FromStartOfDay (min x' y')

    let normalize x y = 
        let normalize x y = if y = 0 then 0.0 else (float x) / (float y)
        let (TimeOfDay.FromStartOfDay x') = x
        let (TimeOfDay.FromStartOfDay y') = y
        normalize x' y'


module DayStartAndEndTimes =
    
    let max (x: DayLength) (y : DayLength) =
        {
            DayLength.startTime = StartAndEndTime.min x.startTime y.startTime
            DayLength.endTime = StartAndEndTime.max x.endTime y.endTime
        }
        
    
