
namespace BucklingSprings.Aware.Deep

open System
open System.Threading

open BucklingSprings.Aware
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions
open Nessos.UnionArgParser

module Program =

    type Arguments =
        | SampleScreen
        | ContainerFile of string
        | TimeOfDayInMinutes of int
    with
        interface IArgParserTemplate with
            member s.Usage = 
                match s with
                    | Arguments.SampleScreen _ -> "Should capture a screen sample."
                    | Arguments.ContainerFile _ -> "Where should the sample data be stored."
                    | Arguments.TimeOfDayInMinutes _ -> "Timestamp for the sample"

    
    let nowInMinutes () =
        let n = DateTimeOffset.Now
        n.MinsSinceStartOfDay
        
    [<EntryPoint>]
    let main argv = 
        Diagnostics.initialize EventLogSource.DeepSamples
        let createdNew = ref false
        use mutex = new Mutex(true, "37B40A45-B4F2-4969-B16D-F3E3BFE59617", createdNew)
        if (!createdNew) then
            let parser = UnionArgParser.Create<Arguments>()
            try
                let results = parser.ParseCommandLine(argv)
                let sampleScreen = results.Contains(<@ Arguments.SampleScreen @>)
                let storeInContainer = results.Contains(<@ Arguments.ContainerFile @>)
                let container = if results.Contains(<@ Arguments.ContainerFile @>) then
                                    Some (results.GetResult(<@ Arguments.ContainerFile @>))
                                else
                                    None

                let timeOfDay = results.GetResult(<@ Arguments.TimeOfDayInMinutes @>, nowInMinutes())

                if sampleScreen then
                    let screenSample =   BinaryStore.tempFileNameFor DeepSampleType.ScreenCapture
                
                    DeepInspection.sampleScreenImage screenSample
                    match container with
                        | None -> ()
                        | Some containerFile ->
                            BinaryStore.addDataFrom containerFile  screenSample DeepSampleType.ScreenCapture timeOfDay
                            System.IO.File.Delete screenSample
                    
                0 // return an integer exit code

            with
                | a -> 
                    Diagnostics.writeToLog EventLogger.DeepSamples EntryLevel.Error "Deep sampling error: %s" (a.Message)
                    -1
        else
            -2
       
