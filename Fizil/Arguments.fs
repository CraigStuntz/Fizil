﻿module Arguments

open Log


type Operation = 
    | Initialize
    | ExecuteTests
    | ReportVersion
    | ShowHelp


type Arguments = {
    Operation:       Operation
    ProjectFileName: string
    Verbosity:       Verbosity
}


let defaultArguments = 
    {
        Operation       = ExecuteTests
        ProjectFileName = "project.yaml"
        Verbosity       = Standard
    }


let helpString (arguments: Arguments) = 
    """Usage: Fizil [OPTION] [path/to/project.yaml]
  --help    Display this help message
  --init    Create working directories
  --quiet   Suppress all non-error command line output
  --verbose Display additional status information on command line output
  --version Report version
  
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
        -> parseArgument { accum with Operation = Initialize }    rest
    | "--quiet"   :: rest 
        -> parseArgument { accum with Verbosity = Quiet }         rest
    | "--verbose" :: rest 
        -> parseArgument { accum with Verbosity = Verbose }       rest
    | "--version" :: rest 
        -> parseArgument { accum with Operation = ReportVersion } rest
    | ProjectFile filename :: rest 
        -> parseArgument { accum with ProjectFileName = filename } rest    
    | "--help"    :: rest 
    | _           :: rest
        -> parseArgument { accum with Operation = ShowHelp }      rest


let parse (argv: string[]) = 
    parseArgument defaultArguments (argv |> List.ofArray)