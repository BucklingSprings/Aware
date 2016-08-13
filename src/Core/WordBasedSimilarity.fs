namespace BucklingSprings.Aware.Core

type Word = string
type WordBasedDocument = Word list

module WordBasedSimilarity =

    let score (testDocument : WordBasedDocument) (categoryDefnitionDocument : WordBasedDocument) : float =
        let matchScore (catWord : string) = 
            let hasExactMatchInTestDocument  =
                let exactMatch = testDocument |> List.tryFind (fun w -> catWord.Equals(w, System.StringComparison.InvariantCultureIgnoreCase))
                Option.isSome exactMatch
            let hasFuzzyMatchInTestDocument  =
                let fuzzyMatch = testDocument |> List.tryFind (fun w -> (w.ToLowerInvariant()).Contains(catWord.ToLowerInvariant()))
                Option.isSome fuzzyMatch

            if hasExactMatchInTestDocument then
                1.0
            elif hasFuzzyMatchInTestDocument then
                0.5
            else
                0.0
        let matches = [ for w in categoryDefnitionDocument -> matchScore w]
        let total = List.sum matches
        total / float (List.length categoryDefnitionDocument)

