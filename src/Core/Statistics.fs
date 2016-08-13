namespace BucklingSprings.Aware.Core.Statistics


[<RequireQualifiedAccess()>]
[<NoComparison()>]
type FiveNumberSummary<'a> =
    {
        maximum: 'a
        upperQuartile : 'a
        median : 'a
        lowerQuartile : 'a
        minimum: 'a
    }

module StatisticalSummary =

    let r = System.Random()

    let zero (z : 'z) : FiveNumberSummary<'z> =
        {
            FiveNumberSummary.maximum = z
            FiveNumberSummary.minimum = z
            FiveNumberSummary.median = z
            FiveNumberSummary.upperQuartile = z
            FiveNumberSummary.lowerQuartile = z
        }

    

    let zerof : FiveNumberSummary<float> = zero 0.0

    let someNegativeValuef : FiveNumberSummary<float> = zero -9999.0

    let someNegativeValue : FiveNumberSummary<int> = zero -999
        
    let normalize (fn : FiveNumberSummary<float>)  maxValue = 
        let relativeTo v = if maxValue = 0.0 then 0.0 else v / maxValue
        {
            FiveNumberSummary.minimum = relativeTo fn.minimum
            FiveNumberSummary.lowerQuartile = relativeTo fn.lowerQuartile
            FiveNumberSummary.median = relativeTo fn.median
            FiveNumberSummary.upperQuartile = relativeTo fn.upperQuartile
            FiveNumberSummary.maximum = relativeTo fn.maximum
        }
    let deNormalize (fn : FiveNumberSummary<float>)  maxValue = 
        let relativeTo v = v * maxValue
        {
            FiveNumberSummary.minimum = relativeTo fn.minimum
            FiveNumberSummary.lowerQuartile = relativeTo fn.lowerQuartile
            FiveNumberSummary.median = relativeTo fn.median
            FiveNumberSummary.upperQuartile = relativeTo fn.upperQuartile
            FiveNumberSummary.maximum = relativeTo fn.maximum
        }

    let round (fn : FiveNumberSummary<float>)  : FiveNumberSummary<int> = 
        let rint = round >> int
        {
            FiveNumberSummary.minimum = rint fn.minimum
            FiveNumberSummary.lowerQuartile = rint fn.lowerQuartile
            FiveNumberSummary.median = rint fn.median
            FiveNumberSummary.upperQuartile = rint fn.upperQuartile
            FiveNumberSummary.maximum = rint fn.maximum
        }

    // FIXME - Delete
    let maximumMaximum (xs : FiveNumberSummary<float> list) =
        if List.isEmpty xs then
            0.0
        else
            (xs |> List.maxBy (fun f -> f.maximum)).maximum


    let summarize (xs : int list) : FiveNumberSummary<float> =
        
        if List.isEmpty xs then
            zerof
        else
            
            
            let ordered = Array.ofList xs
            let count = ordered.Length
            Array.sortInPlace ordered
            let min = float ordered.[0]
            let max = float ordered.[ordered.Length-1]
            let isEven i = i % 2 = 0
            let listMedian startIndex endIndex (xs : int[]) =
                let endIndex = if endIndex < 0 then 0 else endIndex
                let startIndex = if startIndex > endIndex then endIndex else startIndex

                
                let avg x y = ((float x) + (float y)) / 2.0
                let length = endIndex - startIndex + 1
                if isEven length then
                    avg (xs.[(length / 2) - 1 + startIndex]) (xs.[length / 2 + startIndex])
                else
                    float xs.[(length / 2) + startIndex]
            let median = listMedian 0 (ordered.Length - 1) ordered
            let lq = listMedian 0 ((count / 2) - 1) ordered
            let uq = if isEven count then
                        listMedian (count / 2)  (ordered.Length - 1) ordered
                     else
                        listMedian ((count / 2) + 1)  (ordered.Length - 1) ordered
            
            {
                FiveNumberSummary.maximum = max
                FiveNumberSummary.minimum = min
                FiveNumberSummary.median = median
                FiveNumberSummary.upperQuartile = uq
                FiveNumberSummary.lowerQuartile = lq
            }

    let randomForDesignTime (max : int) : FiveNumberSummary<float> =
        let randomNumbers () = 
            Seq.init 10 (fun _ -> r.Next(1,max)) |> Seq.toList
        summarize (randomNumbers ()) 

    let randomForDesignTimeNormalized () : FiveNumberSummary<float> =
        let max = 10
        let randomNumbers () = 
            Seq.init 10 (fun _ -> r.Next(1,max)) |> Seq.toList
        let fns = summarize (randomNumbers ()) 
        normalize fns (float max)


