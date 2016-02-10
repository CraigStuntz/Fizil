module Execute

open System.Diagnostics
open ExecutionResult
open Project


let initialize (project: Project) =
    let workingDirectory = System.IO.Path.Combine [| project.Directories.ProjectDirectory; project.Directories.SystemUnderTest |]
    System.IO.Directory.SetCurrentDirectory(workingDirectory)


let executeApplication (project: Project) (testCase: string) =
    use proc = new Process()
    proc.StartInfo.FileName               <- project.Executable
    proc.StartInfo.Arguments              <- testCase
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
