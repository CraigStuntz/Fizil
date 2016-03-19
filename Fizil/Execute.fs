module Execute

open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.IO
open Fizil.Properties
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


let private checkProperties (propertyCheckers: IPropertyChecker list) (testRun: TestRun) : PropertyCheckResult list =
    match testRun.ExitCode with
    | ExitCodes.success -> 
        propertyCheckers 
            |> List.collect (fun propertyChecker -> propertyChecker.SuccessfulExecutionProperties |> List.ofSeq) 
            |> List.map (fun prop -> prop.CheckSuccessfulExecution testRun )
    | _ -> []


let private executeApplication (executablePath: string) (propertyCheckers: IPropertyChecker list) (testCase: TestCase) =
    use proc = new Process()
    proc.StartInfo.FileName               <- executablePath
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

    let testRun = {
        Input    = testCase.Data
        ExitCode = exitCode
        StdErr   = err
        StdOut   = output
    }
    let propertyViolations = 
        checkProperties propertyCheckers testRun
        |> List.filter (fun propertyCheckResult -> not <| propertyCheckResult.Verified )
    proc.Close()
    {
        StdErr             = err
        StdOut             = output
        ExitCode           = exitCode
        Crashed            = exitCode = WinApi.ClrUnhandledExceptionCode
        NewPathFound       = false
        PropertyViolations = propertyViolations
    }


let toHexString (bytes: byte[]) : string =
    let sBuilder = System.Text.StringBuilder()
    bytes |> Array.iter (fun b -> (sBuilder.Append(b.ToString("x2")) |> ignore))
    sBuilder.ToString()


let private getHash (hash: System.Security.Cryptography.HashAlgorithm) (bytes: byte[]) =
    hash.ComputeHash(bytes)
        |> toHexString


let private getPropertyCheckers() : IPropertyChecker list =
    [ new Fizil.Test.TestProperties() :> IPropertyChecker ]


let private formatPropertyViolations (violations: PropertyCheckResult list) =
    let header = "Property violations: "
    let violationMessages = 
        violations 
        |> List.map (fun violation -> violation.Message)
        |> String.concat System.Environment.NewLine
    sprintf "%s%s%s" header System.Environment.NewLine violationMessages


let private hasPropertyViolations (result : Result) = 
    not <| List.isEmpty result.PropertyViolations


let private executeApplicationTestCase (log: Logger) (executablePath) (state: ExecutionState) (testCase: TestCase) =
    log Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString))
    use sharedMemory = SharedMemory.create()
    let result = executeApplication executablePath (getPropertyCheckers()) testCase
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
    if (resultWithNewPaths |> hasPropertyViolations)
    then log Verbose (resultWithNewPaths.PropertyViolations |> formatPropertyViolations)
    { state with
        Results = resultWithNewPaths :: state.Results
    }


let private logResults (log: Logger) (results: ExecutionResult.Result list) =
    log Standard "Execution complete"
    let testRuns   = results |> List.length
    let crashes    = results |> List.filter (fun result -> result.Crashed)      |> List.length
    let paths      = results |> List.filter (fun result -> result.NewPathFound) |> List.length
    let violations = results |> List.filter hasPropertyViolations               |> List.length
    log Standard (sprintf "  Total runs:                %i" testRuns)
    log Standard (sprintf "  Crashes:                   %i" crashes)
    log Standard (sprintf "  Total paths:               %i" paths)
    log Standard (sprintf "  Total property violations: %i" violations)



let allTests (log: Logger) (project: Project) =
    match loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        let sutExe          = System.IO.Path.Combine(project.Directories.SystemUnderTest, project.Executable)
        let instrumentedExe = System.IO.Path.Combine(project.Directories.Instrumented, project.Executable)
        let executablePath        = if System.IO.File.Exists instrumentedExe then instrumentedExe else sutExe
        initializeTestRun project
        log Standard (sprintf "Testing %s" (System.IO.Path.GetFullPath executablePath))
        let testCases = examples |> Fuzz.all
        use md5 = System.Security.Cryptography.MD5.Create()
        let initialState = {
            Hash          = md5
            ObservedPaths = HashSet<string>()
            Results       = []
        }
        let finalState = 
            testCases
                |> Seq.fold (executeApplicationTestCase log executablePath) initialState
        logResults log finalState.Results
        ExitCodes.success
