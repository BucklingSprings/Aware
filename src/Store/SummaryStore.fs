namespace BucklingSprings.Aware.Store

open BucklingSprings.Aware.Core.StoredSummaries
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open BucklingSprings.Aware.Store
open BucklingSprings.Aware.Entitities

open System.Linq
open System.Data.Entity

module SummaryStore =

    let allSummaries () =
        use ctx = new AwareContext(Store.connectionString)
        (query {
            for s in ctx.Summaries.Include(fun (s : StoredSummary) -> s.summaryClass) do
            select s
        }) |> Seq.toList

    let datesWithSummaries summarySetVersion = 
        use ctx = new AwareContext(Store.connectionString)
        let st = StoredSummaries.machinize StoredSummaries.summaryCalculatedSummary
        let v = StoredSummariesSerializer.writeSummarySetVersion summarySetVersion
        let dates =
            query {
                for s in ctx.Summaries do
                where (s.summaryType = st && s.summary = v)
                select s.summaryTimeAndDate
            }
        Set(dates)

    let storeDaySummaries (d : System.DateTimeOffset) (newSummaries : StoredSummary list) =
        use ctx = new AwareContext(Store.connectionString)
        let startOfDay = d.StartOfDay
        let endOfDay = d.EndOfDay
        let allSummariesForDay =
            (query {
                for s in ctx.Summaries do
                where (StoredSummaries.allDailySummaryTypesMachinized.Contains(s.summaryType) && s.summaryTimeAndDate >= startOfDay && s.summaryTimeAndDate <= endOfDay)
                select s
            })
        allSummariesForDay |> Seq.iter (fun s -> ctx.Summaries.Remove(s) |> ignore)
        newSummaries |> Seq.iter (fun s -> 
                                            if not ( Operators.Unchecked.equals s.summaryClass null) then
                                                ctx.Classes.Attach(s.summaryClass) |> ignore
                                            ctx.Summaries.Add(s) |> ignore)
        ctx.SaveChanges() |> ignore
        
