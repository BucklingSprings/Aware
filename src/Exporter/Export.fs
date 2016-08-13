namespace BucklingSprings.Aware.Exporter


open System

module Export =
    let escape (s : string) =
        if s.Contains("\"") then
            s.Replace("\"","\"\"")
        else
            s

    let formatDate (d : DateTimeOffset) = d.ToString("yyyy-MM-dd HH:mm:ss")

    let formatDateOnly (d : DateTimeOffset) = d.ToString("yyyy-MM-dd")
    let formatDateOnly' (d : DateTime) = d.ToString("yyyy-MM-dd")

