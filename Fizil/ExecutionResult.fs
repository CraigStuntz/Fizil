module ExecutionResult

open Fizil.Properties

type Result = 
    {
        TestCase:           TestCase.TestCase
        StdErr:             string
        StdOut:             string
        ExitCode:           int
        Crashed:            bool
        PropertyViolations: PropertyCheckResult list
        SharedMemory:       byte []
        NewPathFound:       bool
    }
    member this.HasStdErrOutput =
        System.String.IsNullOrWhiteSpace(this.StdOut)

