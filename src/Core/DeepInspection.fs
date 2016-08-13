namespace BucklingSprings.Aware.Core

open System
open System.IO

open System.Drawing
open System.Windows.Forms

open System.IO.Compression

open BucklingSprings.Aware.Core.CommonExtensions.DateTimeOffsetExtensions

type DeepSampleType =
    | ScreenCapture

type DeepInspectionConfig =
    {
        minSpaceInGig : int
        root : string
        screenCaptureEnabled : bool
    }

module DeepInspectionConfigStore =

    let productName = match Environment.currentEnvironment with
                                | Environment.Development -> "AwareDev"
                                | _ -> "Aware"

    let key = sprintf "HKEY_CURRENT_USER\\Software\\BucklingSprings\\%s\\DeepInspection" productName

    let intValue (valueName : string) (defaultValue : int) : int =
        let value = Microsoft.Win32.Registry.GetValue(key, valueName, null)
        if value <> null then
            downcast value
        else
            defaultValue

    let strValue (valueName : string) (defaultValue : string) : string =
        let value = Microsoft.Win32.Registry.GetValue(key, valueName, null)
        if value <> null then
            downcast value
        else
            defaultValue

    let setIntValue (valueName : string) (value : int)  =
        Microsoft.Win32.Registry.SetValue(key, valueName, value)

    let setStrValue (valueName : string) (value : string)  =
        Microsoft.Win32.Registry.SetValue(key, valueName, value)

    let isEnabled () = (intValue "enabled" 0) = 1

    let enableDeepInspection (cfg : DeepInspectionConfig) =
        setIntValue "enabled" 1
        setIntValue "minSpaceInGig" cfg.minSpaceInGig
        setIntValue "screenCaptureEnabled" (if cfg.screenCaptureEnabled then 1 else 0)
        setStrValue "archiveLocation" cfg.root

    let disableDeepInspection () = setIntValue "enabled" 0

    let deepInspectionCurrentConfig () = 
             {
                    minSpaceInGig = intValue "minSpaceInGig" 1
                    screenCaptureEnabled = intValue "screenCaptureEnabled" 0 = 1
                    root = strValue "archiveLocation" Environment.defaultDeepInspectionStoreRoot
             }
        

    let deepInspectionConfig () =
        let enabled = (intValue "enabled" 0) = 1
        if enabled then
            Some (deepInspectionCurrentConfig ())
        else
            None

    
    
    

module BinaryStore  =


    let createContainer (container : string) = 
        let temp = Path.GetTempPath()
        let unq = Guid.NewGuid()
        let folder = unq.ToString()
        let emptyFolder = Path.Combine(temp, folder)
        Directory.CreateDirectory emptyFolder |> ignore
        ZipFile.CreateFromDirectory(emptyFolder, container, CompressionLevel.NoCompression, false)
        Directory.Delete emptyFolder
     
    let entryName (sampleType : DeepSampleType) (minBegOfDay : int) =
        match sampleType with
            | DeepSampleType.ScreenCapture -> (sprintf "screen_%d.jpg" minBegOfDay)
            
    let addToExistingContainer (container : string) (file : string) (sampleType : DeepSampleType) (minBegOfDay : int)  =
        let entryName = entryName sampleType minBegOfDay
        use archive = ZipFile.Open(container, ZipArchiveMode.Update)
        let existingEntry = archive.GetEntry(entryName)
        if existingEntry <> null then
            existingEntry.Delete()
        archive.CreateEntryFromFile(file, entryName) |> ignore


    let tempFileNameFor (sampleType : DeepSampleType) =
        let temp = Path.GetTempPath()
        let unq = Guid.NewGuid()
        let extension = match sampleType with
                            | DeepSampleType.ScreenCapture -> "jpg"
        let fileName = sprintf "%s.%s" (unq.ToString()) extension
        Path.Combine(temp, fileName)

    let addDataFrom (container : string) (file : string) (sampleType : DeepSampleType) (minBegOfDay : int)  =
        let day = TimeSpan.FromDays(1.0)
        if minBegOfDay < 0 || (float minBegOfDay) > day.TotalMinutes then
            raise (ArgumentOutOfRangeException("minBegOfDay"))
        if not (File.Exists(container)) then
            createContainer(container)

        addToExistingContainer container file sampleType minBegOfDay

module DeepInspection =

    let sampleScreenImage (storeInFile : string) =
        if File.Exists storeInFile then
            File.Delete storeInFile
        let bounds = Screen.PrimaryScreen.Bounds
        use bmp = new Bitmap(bounds.Width, bounds.Height)
        use g = Graphics.FromImage(bmp)
        g.CopyFromScreen(bounds.X, bounds.Y,0,0,bmp.Size, CopyPixelOperation.SourceCopy)
        bmp.Save(storeInFile, Imaging.ImageFormat.Jpeg)
        ()

    let datedRoot  (sampleTime : DateTimeOffset) (containerRoot : string) =
        let yrPrefix = let y = sampleTime.Year in y.ToString()
        let dayPrefix = let d = sampleTime.DayOfYear in d.ToString()
        Path.Combine(containerRoot, yrPrefix, dayPrefix)
        

    let deepSamplingArgs (sampleTime : DateTimeOffset) (containerRoot : string) =
        let containerDatedRoot = datedRoot sampleTime containerRoot
        if not (Directory.Exists containerDatedRoot) then
            (Directory.CreateDirectory containerDatedRoot) |> ignore
        let container = Path.Combine(containerDatedRoot, "data.zip")
        sprintf "--containerfile %s --samplescreen --timeofdayinminutes %d" container (sampleTime.MinsSinceStartOfDay)
        

    let sampleOutOfProcess (sampleTime : DateTimeOffset) (containerRoot : string) =
        let executablePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        let fileName = "deep.exe"


        let exe = if Environment.currentEnvironment = Environment.Development then
                    System.IO.Path.Combine(executablePath, "..\\..\\..\\deep\\bin\\Debug\\", fileName)
                  else
                    System.IO.Path.Combine(executablePath, fileName)

        if File.Exists exe then
            use prc = new System.Diagnostics.Process()
            prc.StartInfo <- System.Diagnostics.ProcessStartInfo(exe)
            prc.StartInfo.Arguments <- deepSamplingArgs sampleTime containerRoot
            prc.Start() |> ignore
            ()
        ()

    let exportSamplesForDayTo (dt : DateTimeOffset) (rootFolder : string) =
        let cfg' = DeepInspectionConfigStore.deepInspectionConfig()
        if Option.isSome cfg' then
            let cfg = Option.get cfg'
            let containerDatedRoot = datedRoot dt cfg.root
            let container = Path.Combine(containerDatedRoot, "data.zip")
            if File.Exists container then
                ZipFile.ExtractToDirectory(container,rootFolder)
        ()
    
    let sampleAll (sampleTime : DateTimeOffset) =
        let cfg' = DeepInspectionConfigStore.deepInspectionConfig()
        if Option.isSome cfg' then
            let cfg = Option.get cfg'
            if cfg.screenCaptureEnabled && not (Directory.Exists cfg.root) then
                // The root folder should exists since we are going to check available space in the folder
                Directory.CreateDirectory cfg.root |> ignore
            if cfg.screenCaptureEnabled && DiskSpace.available cfg.minSpaceInGig cfg.root then
                sampleOutOfProcess sampleTime cfg.root
        ()


