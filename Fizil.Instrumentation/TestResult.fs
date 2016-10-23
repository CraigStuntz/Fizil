namespace Fizil.Instrumentation

// This type should be kept C# friendly!
type TestResult(crashed: bool, exitCode: int, stdErr: string, stdOut: string) = 
    member this.Crashed = crashed
    member this.ExitCode = exitCode
    member this.StdErr = stdErr
    member this.StdOut = stdOut
    static member UnhandledException(message: string) = TestResult(true, -532462766, message, "")

