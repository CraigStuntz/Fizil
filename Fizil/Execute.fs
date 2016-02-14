module Execute

open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.IO
open ExecutionResult
open Log
open Project
open TestCase

[<NoComparison>]
type private ExecutionState = {
    /// Actual shared memory value from previous test run.
    /// Compare current test run final value with this to determine if 
    /// fuzzer found any new paths.
    Hash:          System.Security.Cryptography.HashAlgorithm
    ObservedPaths: HashSet<string>
    Results:       Result list
}

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
        StdErr       = err
        StdOut       = output
        ExitCode     = exitCode
        Crashed      = exitCode = WinApi.ClrUnhandledExceptionCode
        NewPathFound = false
    }


let toHexString (bytes: byte[]) : string =
    let sBuilder = System.Text.StringBuilder()
    bytes |> Array.iter (fun b -> (sBuilder.Append(b.ToString("x2")) |> ignore))
    sBuilder.ToString()


let private getHash (hash: System.Security.Cryptography.HashAlgorithm) (bytes: byte[]) =
    hash.ComputeHash(bytes)
        |> toHexString


let private executeApplicationTestCase (log: Logger) (project: Project) (state: ExecutionState) (testCase: TestCase) =
    log Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString))
    use sharedMemory = SharedMemory.create()
    let result = executeApplication project testCase
    let finalSharedMemory = sharedMemory |> SharedMemory.readBytes 
    let hashed = getHash state.Hash finalSharedMemory
    let newPathFound = state.ObservedPaths.Add hashed // mutation! scary!
    let resultWithNewPaths = { result with NewPathFound = newPathFound }
    if (resultWithNewPaths.Crashed)
    then log Standard "Process crashed!"
    if (resultWithNewPaths.NewPathFound)
    then log Verbose "New path found"
    log Verbose (sprintf "StdOut: %s"    resultWithNewPaths.StdOut)
    log Verbose (sprintf "StdErr: %s"    resultWithNewPaths.StdErr)
    log Verbose (sprintf "Exit code: %i" resultWithNewPaths.ExitCode)
    { state with
        Results = resultWithNewPaths :: state.Results
    }


let private logResults (log: Logger) (results: ExecutionResult.Result list) =
    log Standard "Execution complete"
    let testRuns = results |> List.length
    let crashes  = results |> List.filter (fun result -> result.Crashed)      |> List.length
    let paths    = results |> List.filter (fun result -> result.NewPathFound) |> List.length
    log Standard (sprintf "  Total runs:  %i" testRuns)
    log Standard (sprintf "  Crashes:     %i" crashes)
    log Standard (sprintf "  Total paths: %i" paths)



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
        use md5 = System.Security.Cryptography.MD5.Create()
        let initialState = {
            Hash          = md5
            ObservedPaths = HashSet<string>()
            Results       = []
        }
        let finalState = 
            testCases
                |> Seq.fold (executeApplicationTestCase log project) initialState
        logResults log finalState.Results
        ExitCodes.success
