module Log

type Verbosity = 
    | Verbose
    | Standard
    | Quiet


type Logger = Verbosity -> string -> unit


let private shouldLog (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) =
    match argumentsVerbosity with
    | Verbose  -> true
    | Standard -> [ Verbose; Standard] |> List.contains messageVerbosity
    | Quiet    -> messageVerbosity = Quiet


let private log (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) (message: string) =
    match shouldLog argumentsVerbosity messageVerbosity with
    | true  -> printfn "%s" message
    | false -> ()


let create (argumentsVerbosity: Verbosity) : Logger =
    log argumentsVerbosity


let error (message: string) =
    eprintfn "Error: %s" message

