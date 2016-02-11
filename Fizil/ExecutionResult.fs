module ExecutionResult

type Result = 
    {
        StdErr:   string
        StdOut:   string
        ExitCode: int
        Crashed:  bool
    }
    member this.HasStdErrOutput =
        System.String.IsNullOrWhiteSpace(this.StdOut)

