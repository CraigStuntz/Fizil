module Execute

open System.Collections.Generic
open System.Security.Cryptography
open System.Diagnostics
open System.Linq
open System.IO
open FSharp.Collections.ParallelSeq
open Fizil.Instrumentation
open ExecutionResult
open Log
open Project
open TestCase
open System.Linq.Expressions


let (|InProcessSerial|OutOfProcess|) = function
| "InProcessSerial"   -> InProcessSerial
| "OutOfProcess"      -> OutOfProcess
| wrong          -> failwithf "Unexpected Execution Isolation value %s" wrong


[<AbstractClass>]
type private TestRunner (project: Project) =
    let executablePath = 
        let sutExe          = Path.Combine(project.Directories.SystemUnderTest, project.Execute.Executable)
        let instrumentedExe = Path.Combine(project.Directories.Instrumented, project.Execute.Executable)
        if File.Exists instrumentedExe then instrumentedExe else sutExe
    
    member this.ExecutablePath
        with get() = executablePath


    abstract member ExecuteTest: TestCase -> Result
    abstract member Dispose: unit -> unit
    default this.Dispose() = ()
    interface System.IDisposable with 
        member this.Dispose() = this.Dispose()


[<NoComparison>]
[<NoEquality>]
type OutOfProcessInputMethod = {
    BeforeStart: Process -> byte[] -> unit
    AfterStart:  Process -> byte[] -> unit
}


let onCommandLine : OutOfProcessInputMethod = {
    BeforeStart = fun (proc: Process) (data: byte[])  ->
        proc.StartInfo.Arguments <- Convert.toString data
    AfterStart = fun _proc _data -> ()
}

let onStandardInput : OutOfProcessInputMethod = {
    BeforeStart = fun proc _data -> 
        proc.StartInfo.RedirectStandardInput <- true
    AfterStart = fun (proc: Process) (data: byte[])  ->
        proc.StandardInput.BaseStream.Write(data, 0, data.Length)
        proc.StandardInput.Close()
}


let private projectOutOfProcessInputMethod (project: Project) =
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


let private executionId = ref 0L
/// returns a unique (for this test run) name each time it's called
let private getSharedMemoryName() =
    sprintf "Fizil-shared-memory-%d" (System.Threading.Interlocked.Increment(executionId))


type private OutOfProcessTestRunner(project: Project) = 
    inherit TestRunner(project)

    member this.InputMethod = 
        project |> projectOutOfProcessInputMethod
        
    member private this.ExecuteOutOfProcess sharedMemoryName testCase = 
        use proc = new Process()
        proc.StartInfo.FileName               <- this.ExecutablePath
        this.InputMethod.BeforeStart proc testCase.Data
        proc.StartInfo.UseShellExecute        <- false
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError  <- true
        proc.StartInfo.EnvironmentVariables.Add(SharedMemory.environmentVariableName, sharedMemoryName)
        let output = new System.Text.StringBuilder()
        let err = new System.Text.StringBuilder()
        proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
        proc.ErrorDataReceived.Add(fun args -> err.Append(args.Data) |> ignore)

        proc.Start() |> ignore
        this.InputMethod.AfterStart proc testCase.Data
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()

        proc.WaitForExit()
        let exitCode = proc.ExitCode

        let crashed = exitCode = WinApi.ClrUnhandledExceptionCode
        proc.Close()
        {
            TestCase           = testCase
            TestResult         = TestResult(crashed, exitCode, err.ToString(), output.ToString())
            SharedMemory       = Array.empty
            NewPathFound       = false
        }

    override this.ExecuteTest testCase =
        let sharedMemoryName = getSharedMemoryName()
        use sharedMemory = SharedMemory.create(sharedMemoryName)
        let result = this.ExecuteOutOfProcess sharedMemoryName testCase
        let finalSharedMemory = sharedMemory |> SharedMemory.readBytes 
        { result with SharedMemory = finalSharedMemory }


[<NoComparison>]
[<NoEquality>]
type EntryPoint = 
| ByteArrayEntryPoint of (byte[] -> TestResult)
| StringEntryPoint    of (string -> TestResult)

type ByteArrayEntryPointDelegate = delegate of byte[] -> TestResult 
type StringEntryPointDelegate    = delegate of string -> TestResult 


type private InProcessTestRunner(project: Project) = 
    inherit TestRunner(project)
    
    let initializeSharedMemory() =
         let sharedMemoryName = getSharedMemoryName()
         System.Environment.SetEnvironmentVariable(SharedMemory.environmentVariableName, sharedMemoryName)
         SharedMemory.create(sharedMemoryName)

    let mutable sharedMemory = initializeSharedMemory()

    do
        Instrument.Open()

    let tryFindValidFizilEntryPoint(fn: System.Reflection.MethodInfo) : EntryPoint option =
        let parameters = fn.GetParameters()
        match fn.IsStatic && parameters.Count() = 1 && fn.ReturnType = typeof<TestResult> with
        | false -> None
        | true  ->
            match parameters.First().ParameterType with 
            | x when x = typeof<byte[]> ->
                let del = System.Delegate.CreateDelegate(typeof<ByteArrayEntryPointDelegate>, fn) :?> ByteArrayEntryPointDelegate
                Some (ByteArrayEntryPoint (fun bytes -> (del.Invoke bytes)))
            | x when x = typeof<string> ->
                let del = System.Delegate.CreateDelegate(typeof<StringEntryPointDelegate>, fn) :?> StringEntryPointDelegate
                Some (StringEntryPoint (fun str -> (del.Invoke str)))
            | _ -> None


    let findInProcessEntryPoint (executablePath: string) : EntryPoint =
        let assembly = System.Reflection.Assembly.LoadFrom executablePath
        let fn = 
            assembly.GetTypes()
                .SelectMany(fun (t: System.Type) -> t.GetMethods().AsEnumerable())
                .Where(fun m -> m.GetCustomAttributes(typeof<FizilEntryPointAttribute>, false).Length > 0)
                .FirstOrDefault()
        match fn with 
        | null -> failwith "Using in process fuzzing requires a public method in the target assembly annotated with FizilEntryPointAttribute. No such method was found."
        | _ ->
            match tryFindValidFizilEntryPoint fn with
            | None -> failwith "FizilEntryPoint function must be a static method with a single argument of type string or byte[] and a result of type TestResult"
            | Some entryPoint -> entryPoint

    member this.EntryPoint = 
        match findInProcessEntryPoint this.ExecutablePath with
        | ByteArrayEntryPoint fn -> fn
        | StringEntryPoint fn -> (fun bytes -> bytes |> Convert.toString |> fn)

    override this.ExecuteTest testCase =
        Instrument.Clear()
        let testResult = this.EntryPoint(testCase.Data)
        let finalSharedMemory = Instrument.ReadBytes()
        {
            TestCase           = testCase
            TestResult         = testResult
            SharedMemory       = finalSharedMemory
            NewPathFound       = false
        }

    override this.Dispose() =
        sharedMemory.Dispose()
        Instrument.Close()


let initializeTestRun (project: Project) =
    Directory.SetCurrentDirectory(project.Directories.SystemUnderTest)
    // Disable error reporting for this process. 
    // That's inherited by child processes, so we don't get slowed by crash reporting.
    // See https://blogs.msdn.microsoft.com/oldnewthing/20160204-00/?p=92972
    WinApi.disableCrashReporting()


let private loadExampleFile (project: Project) (filename: string) : byte[] =
    let extension = (filename |> Path.GetExtension).ToLowerInvariant()
    if project.TextFileExtensions.Any(fun ext -> extension.Equals(ext, System.StringComparison.OrdinalIgnoreCase))
    then File.ReadAllText(filename) |> Convert.toBytesUtf8
    else File.ReadAllBytes(filename)


let private loadExamples (project: Project) : TestCase list =
    Directory.EnumerateFiles(project.Directories.Examples, "*", SearchOption.AllDirectories)
        |> Seq.map (fun filename -> 
            let data = loadExampleFile project filename
            { 
                Data          = data
                FileExtension = Path.GetExtension(filename)
                SourceFile    = Some filename
                Stage         = Fuzz.useOriginalExample data 
            } )
        |> List.ofSeq


let toHexString (bytes: byte[]) : string =
    let sBuilder = System.Text.StringBuilder()
    bytes |> Array.iter (fun b -> (sBuilder.Append(b.ToString("x2")) |> ignore))
    sBuilder.ToString()


let private getHash (hash: System.Security.Cryptography.HashAlgorithm) (bytes: byte[]) =
    hash.ComputeHash(bytes)
        |> toHexString


let private shouldRecordFinding (result: Result) =
    if (result.TestResult.Crashed)
    then
        if (not result.NewPathFound)
        then
            System.Diagnostics.Debug.WriteLine "Not recording finding because new path not found"
        else
            if result.TestCase.SourceFile.IsSome
            then
                System.Diagnostics.Debug.WriteLine "Not recording finding because there is a source file"
    result.TestResult.Crashed && result.NewPathFound && result.TestCase.SourceFile.IsNone


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

[<NoComparison>]
type private Message = 
    | TestComplete     of Result
    | AllTestsComplete of AsyncReplyChannel<ExecutionState>

let private agent (project: Project) (log: Logger) : MailboxProcessor<Message> = 
    MailboxProcessor.Start(fun inbox ->
        let rec loop (state: ExecutionState) = async {
            let! message = inbox.Receive()
            match message with
            | TestComplete result ->
                let hashed       = getHash state.Hash result.SharedMemory
                let newPathFound = state.ObservedPaths.Add hashed
                if (result.TestResult.Crashed)
                then log.ToFile Standard "Process crashed!"
                if (newPathFound)
                then log.ToFile Verbose "New path found"
                log.ToFile Verbose (sprintf "StdOut: %s"    result.TestResult.StdOut)
                log.ToFile Verbose (sprintf "StdErr: %s"    result.TestResult.StdErr)
                log.ToFile Verbose (sprintf "Exit code: %i" result.TestResult.ExitCode)
                let resultWithPathFound =  {result with NewPathFound = newPathFound }
                Display.postResult (Display.UpdateDisplay resultWithPathFound)
                let findingName = maybeRecordFinding project state resultWithPathFound
                let newState = { state with FindingName = findingName }
                return! loop newState
            | AllTestsComplete replyChannel ->
                replyChannel.Reply state
        }
        async {
            let localNow       = System.DateTime.Now
            let findingsFolder = "findings_" + localNow.ToString("yyyy-MM-dd_HH-mm-ss")
            let rec withUniqueFindingsFolder (project: Project) (state: ExecutionState) = 
                if Directory.Exists (findingsFolderName project state)
                then withUniqueFindingsFolder project { state with FindingsFolder = state.FindingsFolder + "_" }
                else state
            use hash = System.Security.Cryptography.MD5.Create()
            let initialState = 
                {
                    Hash           = hash
                    ObservedPaths  = HashSet<string>()
                    FindingName    = 0
                    FindingsFolder = findingsFolder
                } |> withUniqueFindingsFolder project
            do! loop initialState
        })


let private executeApplicationTestCase (runner: TestRunner) (log: Logger) (agent: MailboxProcessor<Message>) (testCase: TestCase) =
    log.ToFile Verbose (sprintf "Test Case: %s" (testCase.Data |> Convert.toString)) 
    let result = runner.ExecuteTest testCase
    agent.Post(TestComplete result)


let allTests (log: Logger) (project: Project) =
    match loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        executionId := 0L
        let sutExe          = Path.Combine(project.Directories.SystemUnderTest, project.Execute.Executable)
        let instrumentedExe = Path.Combine(project.Directories.Instrumented, project.Execute.Executable)
        let executablePath  = if File.Exists instrumentedExe then instrumentedExe else sutExe
        initializeTestRun project
        log.ToFile Standard (sprintf "Testing %s" (System.IO.Path.GetFullPath executablePath))
        let testCases      = Fuzz.all(examples, Dictionary.readFiles project.Dictionaries)
        Display.postResult (Display.InitializeDisplay { 
            StartTime =    System.DateTimeOffset.Now
            ExampleBytes = examples |> List.sumBy (fun example -> example.Data |> Array.length )
            ExampleCount = examples |> List.length
            })
        let agent = agent project log

        let iter =
            match project.Execute.Isolation with 
            | InProcessSerial -> Seq.iter  // Stuff which can't be done in parallel
            | OutOfProcess    -> PSeq.iter // Stuff which  can  be done in parallel

        use testRunner : TestRunner = 
            match project.Execute.Isolation with
            | InProcessSerial -> new InProcessTestRunner(project)    :> TestRunner
            | OutOfProcess    -> new OutOfProcessTestRunner(project) :> TestRunner
        testCases
            |> iter (executeApplicationTestCase testRunner log agent)
        // Tell agent we're done, dispose stuff it holds.
        let _finalState = agent.PostAndReply(fun replyChannel -> AllTestsComplete replyChannel)
        ExitCodes.success
