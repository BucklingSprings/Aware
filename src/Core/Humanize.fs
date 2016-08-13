#nowarn "52"

namespace BucklingSprings.Aware.Core.Utils

open System

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.Statistics
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.DayStartAndEndTimes
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

type IHumanizable =
    abstract humanize : string

module Humanize =

    let time  (d : DateTimeOffset) = d.ToString("h:mm tt").ToUpperInvariant()

    let dayOfWeek i =
        let dow = enum<DayOfWeek>(i)
        dow.ToString().ToUpperInvariant()
    
    let monthAbbrev  (d : DateTimeOffset) =
        match d.Month with
        | 1 -> "Jan."
        | 2 -> "Feb."
        | 3 -> "Mar."
        | 4 -> "Apr."
        | 5 -> "May"
        | 6 -> "June"
        | 7 -> "July"
        | 8 -> "Aug."
        | 9 -> "Sept."
        | 10 -> "Oct."
        | 11 -> "Nov."
        | 12 -> "Dec."
        | _ -> "__"

    let dateAndDay (d : DateTimeOffset) = 
        let dt = d.ToString("d MMMM")
        let now = DateTimeOffset.Now.StartOfDay
        let yesterday = now.AddDays(-1.0)
        let d = if now = d.StartOfDay then
                    "Today"
                elif yesterday = d.StartOfDay then
                    "Yesterday"
                else
                    d.ToString("dddd")
        sprintf "%s (%s)" dt d
    
    let dateTimeOffset (d : DateTimeOffset) = 
        let now = DateTimeOffset.Now.StartOfDay
        let yesterday = now.AddDays(-1.0)
        if now = d.StartOfDay then
            "Today"
        elif yesterday = d.StartOfDay then
            "Yesterday"
        else
            let ts = now.Subtract(d)
            if ts.TotalDays < 7.0 then
                d.DayOfWeek.ToString()
            else
                d.LocalDateTime.ToShortDateString()

    
    let minutesFromStartOfDayAsTime (min : int) =
        let ts = TimeSpan.FromMinutes(float min)
        ts.ToString("hh\:mm")

    let minutesFromStartOfDay min =
        let ts = TimeSpan.FromMinutes(float min)
        if ts.Minutes = 0 then
            sprintf "%d HR" ts.Hours
        elif ts.Hours = 0 then
            sprintf "%d MIN" ts.Minutes
        else
            sprintf "%d HR %d MIN" ts.Hours ts.Minutes

    let timeOfDay tod =
        StartAndEndTime.toMinutesFromStartOfDay tod |> minutesFromStartOfDay
        

    let minutesFromStartOfDayF (min : float) = minutesFromStartOfDay (int min)

    let hoursFromStartOfDayToTimeFormat = 
         function
            | 12 -> "12:00 PM"
            | 24 -> "12:00 AM"
            | am when am < 12 -> sprintf "%02d:00 AM" am
            | pm -> sprintf "%02d:00 PM" (pm - 12)

    let hoursFromStartOfDayToOrdinal (x : int) =  string x

    let hoursFromStartOfDayAsRange (x : int) = sprintf "%s - %s" (hoursFromStartOfDayToTimeFormat x) (hoursFromStartOfDayToTimeFormat (x+1))

    
    let fiveNumbers (str : 'a -> string) (fn : FiveNumberSummary<'a>)  : string =
        sprintf "Max: %s \nUpper Quartile: %s \nMedian: %s \nLower Quartile: %s \nMin: %s" (str fn.maximum) (str fn.upperQuartile) (str fn.median) (str fn.lowerQuartile) (str fn.minimum)

    let roundUpMinutesFromStartOfDay (min : int) =
        //round up to the next even hour
        // bcause we often show hours on the y axis with two guidelines
        if min <= 60 then
            60
        else
            int (ceil (float min / 120.0)) * 120

    let roundUpMinutesFromStartOfDayF (min : float) = roundUpMinutesFromStartOfDay (int (ceil min)) |> float
        

    let roundUpWordCount wc = ((wc / 200) + 1) * 200

    let roundUpWordCountF (wc : float) = roundUpWordCount (int (ceil wc)) |> float

    let roundUpSummary (x : ActivitySummary) =
        {
            ActivitySummary.minuteCount = roundUpMinutesFromStartOfDay x.minuteCount
            ActivitySummary.wordCount = roundUpWordCount x.wordCount
        }

    let roundUpTimeOfDay t = 
        let (TimeOfDay.FromStartOfDay x) = t
        let u = ((x / 15) + 1) * 15
        let minsInDay = 24 * 60
        TimeOfDay.FromStartOfDay(if u > minsInDay then minsInDay else u)

    let roundDownTimeOfDay t = 
        let (TimeOfDay.FromStartOfDay x) = t
        let u = ((x / 15)) * 15
        let minsInDay = 24 * 60
        TimeOfDay.FromStartOfDay(if u > minsInDay then minsInDay else u)

    let roundDayLength (x : DayLength) =
        {
            DayLength.endTime = roundUpTimeOfDay x.endTime
            DayLength.startTime = roundDownTimeOfDay x.startTime
        }
        

