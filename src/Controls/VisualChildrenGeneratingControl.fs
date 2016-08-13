namespace BucklingSprings.Aware.Controls


open BucklingSprings.Aware.Core.Diagnostics

open System.Windows
open System.Windows.Media

[<AbstractClass>]
type VisualChildrenGeneratingControlBase() as control =
    inherit FrameworkElement()
    let visualChildren = VisualCollection(control)
    let mutable lastHoverVisual : Visual = null
    let mutable lastClickedVisual : Visual = null
    let redrawn = new Event<System.EventArgs>()

    do
        control.Initialized.Add (fun e -> control.RedrawInternal())

    
    
    member control.RedrawInternal () =
        control.InvalidateMeasure()
        visualChildren.Clear()
        control.Redraw() |> List.iter (fun v -> visualChildren.Add(v) |> ignore)
        redrawn.Trigger(System.EventArgs.Empty)

    member x.Redrawn = redrawn.Publish
    abstract member CalculateSize : Size    
    abstract member Redraw : Unit -> Visual list
    abstract member OnVisualMouseEnter : Visual -> Unit
    abstract member OnVisualMouseLeave : Visual -> Unit

    default x.OnVisualMouseEnter _ = ()
    default x.OnVisualMouseLeave _ = ()

    override this.OnMouseMove e =
        let pt = e.GetPosition(this)
        let hitTest = VisualTreeHelper.HitTest(this, pt)
        if hitTest <> null then
            let v = hitTest.VisualHit :?> Visual
            if hitTest.VisualHit <> null then
                if lastHoverVisual <> v then
                    if lastHoverVisual <> null then
                        this.OnVisualMouseLeave lastHoverVisual
                    lastHoverVisual <- v
                    this.OnVisualMouseEnter lastHoverVisual

    override this.ArrangeOverride _ = this.CalculateSize
    override this.MeasureOverride _ = this.CalculateSize
    override this.VisualChildrenCount = visualChildren.Count
    override this.GetVisualChild i = visualChildren.[i]

    static member TriggerRedraw (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? VisualChildrenGeneratingControlBase as tm -> tm.RedrawInternal()
        | _ -> ()
    

