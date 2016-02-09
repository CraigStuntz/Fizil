module Execute

open System.Diagnostics
open ExecutionResult
open Options
open TestCase

let executeApplication (options: Options) (testCase: TestCase) =
    let workingDirectory = System.IO.Path.Combine [| options.Directories.ProjectDirectory; options.Directories.SystemUnderTest |]
    System.IO.Directory.SetCurrentDirectory(workingDirectory)
    use proc = new Process()
    proc.StartInfo.FileName         <- options.Application.Executable
    match testCase.Arguments with
        | Some args -> proc.StartInfo.Arguments <- args
        | None      -> () 
    proc.StartInfo.UseShellExecute        <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError  <- true
    proc.Start() |> ignore

    // Synchronously read the standard output of the spawned process. 
    let output = proc.StandardOutput.ReadToEnd()
    let err    = proc.StandardError.ReadToEnd()

    proc.WaitForExit()
    proc.Close()
    {
        StdErr = err
        StdOut = output
    }
