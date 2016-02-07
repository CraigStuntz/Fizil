module Execute

open System.Diagnostics
open Options
open TestCase

let executeApplication (start: Start) (testCase: TestCase) =
    use proc = new Process()
    proc.StartInfo.FileName         <- start.Executable
    match testCase.Arguments with
        | Some args -> proc.StartInfo.Arguments <- args
        | None      -> () 
    proc.StartInfo.WorkingDirectory <- start.WorkingDirectory
    proc.StartInfo.UseShellExecute  <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.Start() |> ignore

    // Synchronously read the standard output of the spawned process. 
    let reader = proc.StandardOutput
    let output = reader.ReadToEnd()

    proc.WaitForExit()
    proc.Close()
    output
