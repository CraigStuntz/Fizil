module Options

open Log

type Operation = 
    | Initialize
    | ExecuteTests
    | ReportVersion
    | ShowHelp

type Directories = {
    SystemUnderTest: string
    Examples: string
}

type Start = {
    Executable: string
    WorkingDirectory: string
}

type Options = {
    Application: Start
    Directories: Directories
    Operation:   Operation
    Verbosity:   Verbosity
}

let private defaultDirectories =
    {
        SystemUnderTest = "system-under-test"
        Examples        = "examples"
    }

let defaultOptions = 
    {
        Application = 
            {
                Executable       = "..\\..\\..\\TinyTest\\bin\\Debug\\TinyTest.exe"
                WorkingDirectory = System.Environment.CurrentDirectory
            }
        Directories = defaultDirectories
        Operation   = ExecuteTests
        Verbosity   = Verbose
    }

let parse (argv: string[]) = 
    defaultOptions