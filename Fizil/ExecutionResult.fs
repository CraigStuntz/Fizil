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
        System.String.IsNullOrWhiteSpace(this.TestResult.StdOut)

