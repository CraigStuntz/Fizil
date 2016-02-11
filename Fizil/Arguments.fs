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
        Verbosity   = Standard
    }


let helpString (arguments: Arguments) = 
    """Usage: Fizil [OPTION]... 
  --help    Display this help message
  --init    Create working directories
  --quiet   Suppress all non-error command line output
  --verbose Display additional status information on command line output
  --version Report version"""


let rec private parseArgument (accum: Arguments) (argv: string list) =
    match argv with 
    | [] 
        -> accum 
    | "--init"   :: rest 
        -> parseArgument { accum with Operation = Initialize }    rest
    | "--quiet"   :: rest 
        -> parseArgument { accum with Verbosity = Quiet }         rest
    | "--verbose" :: rest 
        -> parseArgument { accum with Verbosity = Verbose }       rest
    | "--version" :: rest 
        -> parseArgument { accum with Operation = ReportVersion } rest
    | "--help"    :: rest 
    | _           :: rest
        -> parseArgument { accum with Operation = ShowHelp }      rest


let parse (argv: string[]) = 
    parseArgument defaultArguments (argv |> List.ofArray)