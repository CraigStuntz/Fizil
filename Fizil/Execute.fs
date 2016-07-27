module Execute

open System.Collections.Generic
open System.Security.Cryptography
open System.Diagnostics
open System.Linq
open System.IO
open FSharp.Collections.ParallelSeq
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
    AfterStart = fun _proc _data -> ()
}

let onStandardInput : InputMethod = {
    BeforeStart = fun proc _data -> 
        proc.StartInfo.RedirectStandardInput <- true
    AfterStart = fun (proc: Process) (data: byte[])  ->
        proc.StandardInput.BaseStream.Write(data, 0, data.Length)
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
    Hash:           System.Security.Cryptography.HashAlgorithm
    FindingName:    int
    FindingsFolder: string
    ObservedPaths:  HashSet<string>
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
    Directory.EnumerateFiles(project.Directories.Examples, "*", SearchOption.AllDirectories)
        |> Seq.map (fun filename -> 
            { 
                Data          = loadExampleFile project filename
                FileExtension = Path.GetExtension(filename)
                SourceFile    = Some filename
                Stage         = "calibration"
            } )
        |> List.ofSeq


let private checkProperties (propertyCheckers: IPropertyChecker list) (testRun: TestRun) : PropertyCheckResult list =
    match testRun.ExitCode with
    | ExitCodes.success -> 
        propertyCheckers 
            |> List.collect (fun propertyChecker -> propertyChecker.SuccessfulExecutionProperties |> List.ofSeq) 
            |> List.map (fun prop -> prop.CheckSuccessfulExecution testRun )
    | _ -> []


let private executeApplication (inputMethod: InputMethod) (executablePath: string) (propertyCheckers: IPropertyChecker list) (sharedMemoryName: string) (testCase: TestCase) =
    use proc = new Process()
    proc.StartInfo.FileName               <- executablePath
    inputMethod.BeforeStart proc testCase.Data
    proc.StartInfo.UseShellExecute        <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError  <- true
    proc.StartInfo.EnvironmentVariables.Add(SharedMemory.environmentVariableName, sharedMemoryName)
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
        TestCase           = testCase
        StdErr             = testRun.StdErr
        StdOut             = testRun.StdOut
        ExitCode           = testRun.ExitCode
        Crashed            = testRun.ExitCode = WinApi.ClrUnhandledExceptionCode
        SharedMemory       = Array.empty
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


let private shouldRecordFinding (result: Result) =
    result.Crashed && result.NewPathFound && result.TestCase.SourceFile.IsNone


let private findingsFolderName (project: Project) (state: ExecutionState) =
    Path.Combine(project.Directories.Examples, state.FindingsFolder)


let private forceFindingsDirectory (project: Project) (state: ExecutionState) : unit =
    let directory = findingsFolderName project state
    Directory.CreateDirectory(directory) |> ignore


let private recordFinding (project: Project) (state: ExecutionState) (testCase: TestCase) =
    forceFindingsDirectory project state
    let filename = state.FindingName.ToString() + testCase.FileExtension
    let fullPath = Path.Combine(findingsFolderName project state, filename)
    File.WriteAllBytes(fullPath, testCase.Data)


/// Record finding if needed. 
/// Return name of next finding, in any case
let private maybeRecordFinding (project: Project) (state: ExecutionState) (result: Result) =
    if shouldRecordFinding result 
    then 
        recordFinding project state result.TestCase
        state.FindingName + 1
    else state.FindingName


let private agent (project: Project) (hash: HashAlgorithm) (log: Logger) : MailboxProcessor<Result> = 
    let localNow       = System.DateTime.Now
    let findingsFolder = "findings_" + localNow.ToString("yyyy-MM-dd_HH-mm-ss")
    let rec withUniqueFindingsFolder (project: Project) (initialState: ExecutionState) = 
        if Directory.Exists (findingsFolderName project initialState)
        then withUniqueFindingsFolder project { initialState with FindingsFolder = initialState.FindingsFolder + "_" }
        else initialState
    let initialState = 
        {
            Hash           = hash
            ObservedPaths  = HashSet<string>()
            FindingName    = 0
            FindingsFolder = findingsFolder
        } |> withUniqueFindingsFolder project
    MailboxProcessor.Start(fun inbox ->
        let rec loop (state: ExecutionState) = async {
            let! result = inbox.Receive()
            let hashed = getHash state.Hash result.SharedMemory
            let newPathFound = state.ObservedPaths.Add hashed // mutation! scary!
            if (result.Crashed)
            then log.ToFile Standard "Process crashed!"
            if (newPathFound)
            then log.ToFile Verbose "New path found"
            log.ToFile Verbose (sprintf "StdOut: %s"    result.StdOut)
            log.ToFile Verbose (sprintf "StdErr: %s"    result.StdErr)
            log.ToFile Verbose (sprintf "Exit code: %i" result.ExitCode)
            Display.postResult(result)
            if   (result |> hasPropertyViolations)
            then log.ToFile Verbose (result.PropertyViolations |> formatPropertyViolations)
            let findingName = maybeRecordFinding project state result
            let newState = { state with FindingName = findingName }
            return! loop newState
        }
        loop initialState)


let private executionId = ref 0L
/// returns a unique (for this test run) name each time it's called
let private getSharedMemoryName() =
    sprintf "Fizil-shared-memory-%d" (System.Threading.Interlocked.Increment(executionId))


let private executeApplicationTestCase (log: Logger) (inputMethod: InputMethod) (executablePath) (agent: MailboxProcessor<Result>) (testCase: TestCase) =
    log.ToFile Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString))
    let sharedMemoryName = getSharedMemoryName()
    use sharedMemory = SharedMemory.create(sharedMemoryName)
    let result = executeApplication inputMethod executablePath (getPropertyCheckers()) sharedMemoryName testCase
    let finalSharedMemory = sharedMemory |> SharedMemory.readBytes 
    let resultWithSharedMemory = { result with SharedMemory = finalSharedMemory }
    agent.Post(resultWithSharedMemory)


//let private logResults (log: Logger) (results: ExecutionResult.Result list) =
//    log.ToFile Standard "Execution complete"
//    let testRuns   = results |> List.length
//    let crashes    = results |> List.filter (fun result -> result.Crashed)       |> List.length
//    let paths      = results |> List.filter (fun result -> result.NewPathFound)  |> List.length
//    let violations = results |> List.filter hasPropertyViolations                |> List.length
//    let nonzero    = results |> List.filter (fun result -> result.ExitCode <> 0) |> List.length
//    log.ToFile Standard (sprintf "  Total runs:                %i" testRuns)
//    log.ToFile Standard (sprintf "  Crashes:                   %i" crashes)
//    log.ToFile Standard (sprintf "  Nonzero exit codes:        %i" nonzero)
//    log.ToFile Standard (sprintf "  Total paths:               %i" paths)
//    log.ToFile Standard (sprintf "  Total property violations: %i" violations)


let allTests (log: Logger) (project: Project) =
    match loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        executionId := 0L
        use md5            = System.Security.Cryptography.MD5.Create()
        let sutExe          = Path.Combine(project.Directories.SystemUnderTest, project.Execute.Executable)
        let instrumentedExe = Path.Combine(project.Directories.Instrumented, project.Execute.Executable)
        let inputMethod     = project |> projectInputMethod
        let executablePath  = if File.Exists instrumentedExe then instrumentedExe else sutExe
        initializeTestRun project
        log.ToFile Standard (sprintf "Testing %s" (System.IO.Path.GetFullPath executablePath))
        let testCases      = examples |> Fuzz.all
        let agent = agent project md5 log

        testCases
            |> PSeq.iter (executeApplicationTestCase log inputMethod executablePath agent)
        // logResults log finalState.Results
        ExitCodes.success
