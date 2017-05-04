module ExecutionResult

open Fizil.Instrumentation

[<NoComparison>]
type Result = 
    {
        TestCase:           TestCase.TestCase
        TestResult:         TestResult
        SharedMemory:       byte []
        NewPathFound:       bool
    }
    member this.HasStdErrOutput =
        not <| System.String.IsNullOrWhiteSpace(this.TestResult.StdErr)