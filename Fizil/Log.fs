module Log

open ExecutionResult
open System.IO

type Output = 
    | ToFile

type Verbosity = 
    | Verbose
    | Standard
    | Quiet


[<NoComparison>]
[<NoEquality>]
type Logger = {
    ToFile:    Verbosity -> string -> unit
}


let private shouldLog (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) =
    match argumentsVerbosity with
    | Verbose     -> true
    | Standard    -> [ Standard; Quiet ] |> List.contains messageVerbosity
    | Quiet       -> messageVerbosity = Quiet


let private log (textWriter: System.IO.TextWriter option) (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) (message: string) =
    match textWriter, shouldLog argumentsVerbosity messageVerbosity with
    | Some writer, true -> writer.WriteLine (sprintf "%s" message)
    | _, _           -> ()


let create (textWriter: TextWriter option, argumentsVerbosity: Verbosity) : Logger =
    { 
        ToFile = log textWriter argumentsVerbosity
    }


let error (message: string) =
    eprintfn "Error: %s" message

