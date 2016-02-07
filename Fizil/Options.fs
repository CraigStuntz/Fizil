module Options

type Start = {
    Executable: string
    WorkingDirectory: string
}

type Verbosity = 
    | Verbose
    | Standard
    | Quiet

type Options = {
    Application: Start
    Verbosity:   Verbosity
}