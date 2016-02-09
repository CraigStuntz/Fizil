module Options

open Log

type Operation = 
    | Initialize
    | ExecuteTests
    | ReportVersion
    | ShowHelp

type Directories = {
    SystemUnderTest:  string
    Examples:         string
    Findings:         string
    ProjectDirectory: string
}

type Start = {
    Executable: string
}

type Options = {
    Application: Start
    Directories: Directories
    Operation:   Operation
    Verbosity:   Verbosity
}

let private defaultDirectories =
    {
        SystemUnderTest  = "system-under-test"
        Examples         = "examples"
        Findings         = "findings"
        ProjectDirectory = "..\..\..\Demo" 
    }

let defaultOptions = 
    {
        Application = 
            {
                Executable       = "TinyTest.exe"
            }
        Directories = defaultDirectories
        Operation   = ExecuteTests
        Verbosity   = Verbose
    }

let helpString (options: Options) = 
    """Usage: Fizil [OPTION]... 
  --help    Display this help message
  --init    Create working directories
  --version Report version"""

let rec private parseOption (accum: Options) (argv: string list) =
    match argv with 
    | [] 
        -> accum 
    | "--init" :: rest 
        -> parseOption { accum with Operation = Initialize }    rest
    | "--version" :: rest 
        -> parseOption { accum with Operation = ReportVersion } rest
    | "--help" :: rest 
    | _        :: rest
        -> parseOption { accum with Operation = ShowHelp }      rest


let parse (argv: string[]) = 
    parseOption defaultOptions (argv |> List.ofArray)