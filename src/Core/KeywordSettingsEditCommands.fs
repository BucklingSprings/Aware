namespace BucklingSprings.Aware.Core.Settings

type KeywordSettingsEditCommand =
            | RenameClass of string * string
            | DeleteClass of string
            | DeletePhrase of string * string
            | AddPhrase of string * string
            | AddClass of string

module KeywordSettingsEditCommands =

    let humanize c = 
        match c with
        | RenameClass(f,t) -> sprintf "Rename category from %s to %s" f t
        | DeleteClass(c) -> sprintf "Delete category - %s" c
        | AddClass c -> sprintf "Add category - %s" c
        | DeletePhrase (c,p) -> sprintf "Delete phrase - %s from %s" p c
        | AddPhrase (c,p) -> sprintf "Add phrase - %s to %s" p c

    let requiresVersionChange c =
        match c with
        | RenameClass(_,_) -> false
        | DeleteClass _ -> true
        | AddClass _ -> true
        | DeletePhrase(_,_) -> true
        | AddPhrase (_,_) -> true
        