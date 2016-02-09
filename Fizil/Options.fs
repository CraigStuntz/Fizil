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
    WorkingDirectory: string
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
        WorkingDirectory = System.Environment.CurrentDirectory
    }

let defaultOptions = 
    {
        Application = 
            {
                Executable       = "..\\..\\..\\TinyTest\\bin\\Debug\\TinyTest.exe"
            }
        Directories = defaultDirectories
        Operation   = ExecuteTests
        Verbosity   = Verbose
    }

let rec private parseOption (accum: Options) (argv: string list) =
    match argv with 
    | [] 
        -> accum 
    | "--init" :: rest 
        -> parseOption { accum with Operation = Initialize } rest
    | "--version" :: rest 
        -> parseOption { accum with Operation = ReportVersion } rest
    | "--help" :: rest 
    | _        :: rest
        -> parseOption { accum with Operation = ShowHelp } rest


let parse (argv: string[]) = 
    parseOption defaultOptions (List.ofArray argv)