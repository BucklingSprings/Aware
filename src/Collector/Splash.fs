namespace BucklingSprings.Aware.Collector

open System.Windows.Forms


type DummyType = DummyType

module SplashForm =

    type InvokeDelegate = delegate of Unit -> Unit

    let instance =
        let w = new Form()
        let ni = new NotifyIcon()

        w.FormBorderStyle <- FormBorderStyle.None
        w.BackColor <- System.Drawing.Color.White
        w.Height <- 2
        w.Width <- 2

        
        w.StartPosition <- FormStartPosition.Manual
        w.Top <- 0
        w.Left <- 0

        ni.Text <- "Aware Collector"
        ni.Icon <- new System.Drawing.Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Tray.ico"))
        ni.Visible <- true

        w.Activated.Add(fun e -> w.Hide())

        let showMessage (s : string) =
            w.Invoke(new InvokeDelegate(fun _ -> ni.ShowBalloonTip(30, "Aware", s, ToolTipIcon.Error))) |> ignore

        w, showMessage
