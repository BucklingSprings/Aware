namespace BucklingSprings.Aware.Upgrade


open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Store


open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Documents

module AwareInitialization =

    let launch target =
        let executablePath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        let fileName, args = match target with
                                | LaunchType.Aware -> "BucklingSprings.Aware.exe", ""
                                | LaunchType.Collector -> "BucklingSprings.Aware.Collector.exe", "--no-collection-delay"



        let exe = if Environment.currentEnvironment = Environment.Development then
                    System.IO.Path.Combine(executablePath, "..\\..\\..\\Aware\\bin\\Debug\\", fileName)
                    else
                    System.IO.Path.Combine(executablePath, fileName)

        use prc = new System.Diagnostics.Process()
        prc.StartInfo <- System.Diagnostics.ProcessStartInfo(exe)
        prc.StartInfo.Arguments <- args
        prc.Start() |> ignore
        ()

    let launchAll targets = List.iter launch targets

    let initialize emitStatus emitVersion closeScreen targets noLaunch displayVersion = 
        async {
            emitStatus "Initializing data storage..."
            Store.initialize (true)
            emitStatus "Initializing data storage...Done."

            emitStatus "Creating classifiers..."
            let progamClassifier =  ClassifierStore.ensureExistsByType (Classifier(classifierName = "Programs", classifierType = PrimitiveConstants.programClassifierType))
            let categoryClassifier =  ClassifierStore.ensureExistsByType (Classifier(classifierName = "Categories", classifierType = PrimitiveConstants.categoryClassifierType, limitToMostUsed = false))


        
            ClassifierStore.ensureCatchAllClassExists categoryClassifier |> ignore
            ClassifierStore.ensureIdleClassExists categoryClassifier |> ignore
            ClassifierStore.ensureIdleClassExists progamClassifier |> ignore

            emitStatus "Creating classifiers...Done."
            emitVersion displayVersion

            let finalStatus = 
                match targets  with
                | [LaunchType.Collector] -> "COLLECTOR"
                | _ -> String.Empty

            emitStatus finalStatus

            if not noLaunch then
                launchAll targets

            closeScreen()


            return ()
        }


type WelcomeWindow(targets, noLaunch, displayVersion) as window =
    inherit Window()
    
    let content = Application.LoadComponent(Uri("/BucklingSprings.Aware.Upgrade;component/WelcomeWindow.xaml", UriKind.Relative)) :?> UserControl
    let status = content.FindName("StatusRun") :?> Run
    let versionRun = content.FindName("VersionRun") :?> Run
    let emitStatus = fun s ->  
                        Diagnostics.writeToLog EventLogger.Upgrade EntryLevel.Information "%s" s
                        window.Dispatcher.Invoke(fun _ -> status.Text <- s)
    let emitVersion = fun s ->  window.Dispatcher.Invoke(fun _ -> versionRun.Text <- s)
    let closeScreen = fun () ->  if noLaunch then
                                    window.MouseUp.Add(fun _ -> window.Close())
                                 else
                                    window.Dispatcher.Invoke(fun _ -> window.Close())
    
    do
        window.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        window.Title <- "Aware"
        window.SizeToContent <- SizeToContent.WidthAndHeight
        window.Icon <- BitmapImage(Uri("pack://application:,,,/BucklingSprings.Aware.Upgrade;component/Upgrade.ico", UriKind.Absolute))
        window.Content <- content
        window.AllowsTransparency <- true
        window.Background <- Brushes.Transparent
        window.WindowStyle <- WindowStyle.None
        window.Loaded.Add(fun _ -> Async.Start(AwareInitialization.initialize emitStatus emitVersion closeScreen targets noLaunch displayVersion))
        