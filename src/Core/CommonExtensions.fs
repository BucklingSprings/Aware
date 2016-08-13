#nowarn "52"
namespace BucklingSprings.Aware.Core.CommonExtensions

open System.Linq
open System.Collections.ObjectModel


module DateTimeOffsetExtensions = 

    type System.DateTimeOffset with
        member this.SubtractDays (i : int) = -1.0 * float i |> this.AddDays
        member this.StartOfDay = System.DateTimeOffset(this.Year, this.Month, this.Day, 0, 0, 0, this.Offset)
        member this.StartOfWeek = 
            let dayOffset = System.DayOfWeek.Sunday - this.DayOfWeek
            this.AddDays(float dayOffset).StartOfDay
        member this.StartOfYear = System.DateTimeOffset(this.Year, 1, 1, 0, 0, 0, this.Offset)
        member this.EndOfDay = System.DateTimeOffset(this.Year, this.Month, this.Day, 0, 0, 0, this.Offset).AddDays(1.0).AddSeconds(-1.0)
        member this.EndOfHour = System.DateTimeOffset(this.Year, this.Month, this.Day, this.Hour, 0, 0, this.Offset).AddHours(1.0)
        member this.IsSameDay (that : System.DateTimeOffset) = this.Day = that.Day && this.Month = that.Month && this.Year = that.Year
        member this.MinsSinceStartOfDay =
            let tm = this.Subtract(this.StartOfDay)
            int tm.TotalMinutes

module ObervableCollectionExtensions =

    type ObservableCollection<'a> with
        member x.RemoveAll (predicate : System.Func<'a, bool>) =
            x.Where(predicate) |> Seq.toList |> List.iter (fun o -> x.Remove(o) |> ignore)

