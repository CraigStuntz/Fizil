module ExecutionResult

open Fizil.Properties

type Result = 
    {
        StdErr:             string
        StdOut:             string
        ExitCode:           int
        Crashed:            bool
        PropertyViolations: PropertyCheckResult list
        NewPathFound:       bool
    }
    member this.HasStdErrOutput =
        System.String.IsNullOrWhiteSpace(this.StdOut)

