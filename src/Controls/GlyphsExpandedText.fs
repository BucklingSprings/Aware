namespace BucklingSprings.Aware.Controls.Labels

open System.Windows
open System.Windows.Documents
open System.Text

module Glyphs =

    let setGlyphText (g : Glyphs) (text : string) : Unit =
        g.Indices <- null
        if text = null then
            g.UnicodeString <- " "
        else
            let periodEnding = text.EndsWith(".")
            g.UnicodeString <- text
            let numIndices = text.Length
            let indices = StringBuilder()
            for i = 0 to numIndices - 2 do
                if i = numIndices - 2 && periodEnding then
                    indices.Append(",90;") |> ignore
                else
                    indices.Append(",110;") |> ignore
            g.Indices <- indices.ToString()
        

type GlyphsExpandedText() =
    inherit DependencyObject()



    static let GlyphsExpandedTextProperty =
        DependencyProperty.RegisterAttached(
                                             "GlyphsExpandedText",
                                             typeof<string>,
                                             typeof<GlyphsExpandedText>,
                                             new PropertyMetadata(
                                                System.String.Empty, 
                                                new PropertyChangedCallback(GlyphsExpandedText.UnicodeStringChanged)))

    static member UnicodeStringChanged (d : DependencyObject) (e : DependencyPropertyChangedEventArgs) = 
        match d with
        | :? Glyphs as g -> Glyphs.setGlyphText g (e.NewValue :?> string)
        | _ -> ()

    static member SetGlyphsExpandedText (g : Glyphs, s : string) = g.SetValue(GlyphsExpandedTextProperty, s)
    static member GetGlyphsExpandedText (g : Glyphs) : string = g.GetValue(GlyphsExpandedTextProperty) :?> string
        