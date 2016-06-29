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
[<NoEquality>]
type InputMethod = {
    BeforeStart: Process -> byte[] -> unit
    AfterStart:  Process -> byte[] -> unit
}

let onCommandLine : InputMethod = {
    BeforeStart = fun (proc: Process) (data: byte[])  ->
        proc.StartInfo.Arguments <- Convert.toString data
    AfterStart = fun proc data -> ()
}

let onStandardInput : InputMethod = {
    BeforeStart = fun proc data -> 
        proc.StartInfo.RedirectStandardInput <- true
    AfterStart = fun (proc: Process) (data: byte[])  ->
        proc.StandardInput.Write (Convert.toString data)
        proc.StandardInput.Close()
}


let private projectInputMethod (project: Project) =
    match project.Execute.Input.ToLowerInvariant() with
    | "oncommandline"   -> onCommandLine
    | "onstandardinput" -> onStandardInput
    | _                 -> failwithf "Unrecognized Execute -> Input value %s found in project file" project.Execute.Input


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


let private loadExamples (project: Project) : TestCase list =
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


let private executeApplication (inputMethod: InputMethod) (executablePath: string) (propertyCheckers: IPropertyChecker list) (testCase: TestCase) =
    use proc = new Process()
    proc.StartInfo.FileName               <- executablePath
    inputMethod.BeforeStart proc testCase.Data
    proc.StartInfo.UseShellExecute        <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError  <- true
    let output = new System.Text.StringBuilder()
    let err = new System.Text.StringBuilder()
    proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
    proc.ErrorDataReceived.Add(fun args -> err.Append(args.Data) |> ignore)

    proc.Start() |> ignore
    inputMethod.AfterStart proc testCase.Data
    proc.BeginOutputReadLine()
    proc.BeginErrorReadLine()

    proc.WaitForExit()
    let exitCode = proc.ExitCode

    let testRun = {
        Input    = testCase.Data
        ExitCode = exitCode
        StdErr   = err.ToString()
        StdOut   = output.ToString()
    }
    let propertyViolations = 
        checkProperties propertyCheckers testRun
        |> List.filter (fun propertyCheckResult -> not <| propertyCheckResult.Verified )
    proc.Close()
    {
        StdErr             = testRun.StdErr
        StdOut             = testRun.StdOut
        ExitCode           = testRun.ExitCode
        Crashed            = testRun.ExitCode = WinApi.ClrUnhandledExceptionCode
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


let private executeApplicationTestCase (log: Logger) (inputMethod: InputMethod) (executablePath) (state: ExecutionState) (testCase: TestCase) =
    log Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString))
    use sharedMemory = SharedMemory.create()
    let result = executeApplication inputMethod executablePath (getPropertyCheckers()) testCase
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
    let crashes    = results |> List.filter (fun result -> result.Crashed)       |> List.length
    let paths      = results |> List.filter (fun result -> result.NewPathFound)  |> List.length
    let violations = results |> List.filter hasPropertyViolations                |> List.length
    let nonzero    = results |> List.filter (fun result -> result.ExitCode <> 0) |> List.length
    log Standard (sprintf "  Total runs:                %i" testRuns)
    log Standard (sprintf "  Crashes:                   %i" crashes)
    log Standard (sprintf "  Nonzero exit codes:        %i" nonzero)
    log Standard (sprintf "  Total paths:               %i" paths)
    log Standard (sprintf "  Total property violations: %i" violations)


let allTests (log: Logger) (project: Project) =
    match loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        let sutExe          = Path.Combine(project.Directories.SystemUnderTest, project.Execute.Executable)
        let instrumentedExe = Path.Combine(project.Directories.Instrumented, project.Execute.Executable)
        let inputMethod     = project |> projectInputMethod
        let executablePath  = if File.Exists instrumentedExe then instrumentedExe else sutExe
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
                |> Seq.fold (executeApplicationTestCase log inputMethod executablePath) initialState
        logResults log finalState.Results
        ExitCodes.success
