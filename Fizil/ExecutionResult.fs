module ExecutionResult

type Result = {
    StdErr:   string
    StdOut:   string
    ExitCode: int
    Crashed:  bool
}

