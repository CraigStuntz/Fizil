module Log

type Verbosity = 
    | Verbose
    | Standard
    | Quiet

let private shouldLog (optionsVerbosity: Verbosity) (messageVerbosity: Verbosity) =
    match optionsVerbosity with
    | Verbose  -> true
    | Standard -> [ Verbose; Standard] |> List.contains messageVerbosity
    | Quiet    -> messageVerbosity = Quiet

let log (optionsVerbosity: Verbosity) (messageVerbosity: Verbosity) (message: string) =
    match shouldLog optionsVerbosity messageVerbosity with
    | true  -> System.Console.WriteLine message
    | false -> ()

