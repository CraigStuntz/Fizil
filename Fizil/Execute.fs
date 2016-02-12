module Execute

open System.Diagnostics
open System.Linq
open System.IO
open ExecutionResult
open Log
open Project
open TestCase


let initializeTestRun (project: Project) =
    Directory.SetCurrentDirectory(project.Directories.SystemUnderTest)
    // Disable error reporting for this process. 
    // That's inherited by child processes, so we don't get slowed by crash reporting.
    // See https://blogs.msdn.microsoft.com/oldnewthing/20160204-00/?p=92972
    WinApi.disableCrashReporting()


let private loadExampleFile (project: Project) (filename: string) : byte[] =
    let extension = (filename |> Path.GetExtension).ToLowerInvariant()
    if project.TextFileExtensions.Any(fun ext -> extension.Equals(ext, System.StringComparison.OrdinalIgnoreCase))
    then File.ReadAllText(filename) |> Convert.toBytes
    else File.ReadAllBytes(filename)


let loadExamples (project: Project) : TestCase list =
    Directory.EnumerateFiles(project.Directories.Examples)
        |> Seq.map (fun filename -> 
            { 
                Data          = loadExampleFile project filename
                FileExtension = Path.GetExtension(filename) 
            } )
        |> List.ofSeq


let private ClrUnhandledExceptionCode = -532462766

let executeApplication (project: Project) (testCase: TestCase) =
    use proc = new Process()
    proc.StartInfo.FileName               <- project.Executable
    proc.StartInfo.Arguments              <- Convert.toString testCase.Data
    proc.StartInfo.UseShellExecute        <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError  <- true
    proc.Start() |> ignore

    // Synchronously read the standard output of the spawned process. 
    let output = proc.StandardOutput.ReadToEnd()
    let err    = proc.StandardError.ReadToEnd()

    proc.WaitForExit()
    let exitCode = proc.ExitCode
    proc.Close()
    {
        StdErr   = err
        StdOut   = output
        ExitCode = exitCode
        Crashed  = exitCode = ClrUnhandledExceptionCode
    }


let private executeApplicationTestCase (log: Logger) (project: Project) (testCase: TestCase) =
    log Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString))
    let result = executeApplication project testCase
    if (result.Crashed)
    then log Standard "Process crashed!"
    log Verbose (sprintf "StdOut: %s"    result.StdOut)
    log Verbose (sprintf "StdErr: %s"    result.StdErr)
    log Verbose (sprintf "Exit code: %i" result.ExitCode)
    result


let private logResults (log: Logger) (results: ExecutionResult.Result list) =
    log Standard "Execution complete"
    let testRuns = results |> List.length
    let crashes  = results |> List.filter (fun result -> result.Crashed) |> List.length
    log Standard (sprintf "  Total runs: %i" testRuns)
    log Standard (sprintf "  Crashes:    %i" crashes)


let allTests (log: Logger) (project: Project) =
    match loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        let fullPath = System.IO.Path.Combine(project.Directories.SystemUnderTest, project.Executable)
        initializeTestRun project
        log Standard (sprintf "Testing %s" (System.IO.Path.GetFullPath fullPath))
        let testCases = examples |> Fuzz.all
        let results = 
            testCases
                |> Seq.map (executeApplicationTestCase log project)
                |> List.ofSeq
        logResults log results
        ExitCodes.success
