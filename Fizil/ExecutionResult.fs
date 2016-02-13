module ExecutionResult

type Result = 
    {
        StdErr:       string
        StdOut:       string
        ExitCode:     int
        Crashed:      bool
        NewPathFound: bool
    }
    member this.HasStdErrOutput =
        System.String.IsNullOrWhiteSpace(this.StdOut)

