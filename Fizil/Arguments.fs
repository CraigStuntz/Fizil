module Arguments

open Log


type Operation = 
    | Initialize
    | Instrument
    | ExecuteTests
    | ReportVersion
    | ShowHelp


type Arguments = {
    Operations:      Operation list
    ProjectFileName: string
    Verbosity:       Verbosity
}


let defaultArguments = 
    {
        Operations       = []
        ProjectFileName = "project.yaml"
        Verbosity       = Standard
    }


let helpString () = 
    """Usage: Fizil [OPTION] [path/to/project.yaml]
  --help       Display this help message
  --init       Create working directories
  --instrument Instrument binaries in system under test folder
  --fuzz       Execute tests by fuzzing examples
  --quiet      Suppress all non-error command line output
  --verbose    Display additional status information on command line output
  --version    Report version
  
  If project filename is not specified, Fizil will look for project.yaml in current directory"""


let private (|ProjectFile|_|) str = 
    if (System.IO.File.Exists str)
    then Some str
    else None


let rec private parseArgument (accum: Arguments) (argv: string list) =
    match argv with 
    | [] 
        -> accum 
    | "--init"   :: rest 
        -> parseArgument { accum with Operations = Initialize :: accum.Operations }      rest
    | "--fuzz"   :: rest 
        -> parseArgument { accum with Operations = accum.Operations @ [ ExecuteTests ] } rest
    | "--instrument"   :: rest 
        -> parseArgument { accum with Operations = Instrument :: accum.Operations }      rest
    | "--quiet"   :: rest 
        -> parseArgument { accum with Verbosity = Quiet }                                rest
    | "--verbose" :: rest 
        -> parseArgument { accum with Verbosity = Verbose }                              rest
    | "--version" :: rest 
        -> parseArgument { accum with Operations = ReportVersion :: accum.Operations }   rest
    | ProjectFile filename :: rest 
        -> parseArgument { accum with ProjectFileName = filename }                       rest    
    | "--help"    :: rest 
    | _           :: rest
        -> parseArgument { accum with Operations = ShowHelp :: accum.Operations }        rest


let parse (argv: string[]) = 
     let result = parseArgument defaultArguments (argv |> List.ofArray)
     match result.Operations with
     | [] -> { result with Operations = [ ExecuteTests ] } // default operation
     | _  -> result