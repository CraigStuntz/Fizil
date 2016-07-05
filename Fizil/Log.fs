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


let private log (textWriter: System.IO.StreamWriter option) (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) (message: string) =
    match textWriter, shouldLog argumentsVerbosity messageVerbosity with
    | Some writer, true -> writer.WriteLine (sprintf "%s" message)
    | _, _           -> ()


let create (toStream: Stream option, argumentsVerbosity: Verbosity) : Logger =
    let textWriter = 
        toStream |> Option.bind (fun stream -> Some (new StreamWriter(stream)))
    { 
        ToFile = log textWriter argumentsVerbosity
    }


let error (message: string) =
    eprintfn "Error: %s" message

