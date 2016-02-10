module Arguments

open Log


type Operation = 
    | Initialize
    | ExecuteTests
    | ReportVersion
    | ShowHelp


type Arguments = {
    Operation:   Operation
    Verbosity:   Verbosity
}


let defaultArguments = 
    {
        Operation   = ExecuteTests
        Verbosity   = Verbose
    }


let helpString (arguments: Arguments) = 
    """Usage: Fizil [OPTION]... 
  --help    Display this help message
  --init    Create working directories
  --version Report version"""


let rec private parseArgument (accum: Arguments) (argv: string list) =
    match argv with 
    | [] 
        -> accum 
    | "--init" :: rest 
        -> parseArgument { accum with Operation = Initialize }    rest
    | "--version" :: rest 
        -> parseArgument { accum with Operation = ReportVersion } rest
    | "--help" :: rest 
    | _        :: rest
        -> parseArgument { accum with Operation = ShowHelp }      rest


let parse (argv: string[]) = 
    parseArgument defaultArguments (argv |> List.ofArray)