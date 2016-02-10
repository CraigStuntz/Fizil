module Log

type Verbosity = 
    | Verbose
    | Standard
    | Quiet

let private shouldLog (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) =
    match argumentsVerbosity with
    | Verbose  -> true
    | Standard -> [ Verbose; Standard] |> List.contains messageVerbosity
    | Quiet    -> messageVerbosity = Quiet

let log (argumentsVerbosity: Verbosity) (messageVerbosity: Verbosity) (message: string) =
    match shouldLog argumentsVerbosity messageVerbosity with
    | true  -> System.Console.WriteLine message
    | false -> ()

