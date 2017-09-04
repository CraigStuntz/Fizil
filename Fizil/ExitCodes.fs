module ExitCodes

open Execute

[<Literal>]
let success = 0

[<Literal>]
let internalError = 1

// These are going to change! Don't count on them for now.
[<Literal>]
let projectFileNotFound = 64

[<Literal>]
let examplesNotFound    = 65


let fromSessionResult = function
    | ExamplesNotFound -> examplesNotFound
    | Success -> success