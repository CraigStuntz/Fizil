module Execute

open System.Diagnostics
open System.Linq
open System.IO
open System.Runtime.InteropServices
open ExecutionResult
open Project
open TestCase


[<System.Flags>]
type ErrorModes =
    | SYSTEM_DEFAULT             = 0us
    | SEM_FAILCRITICALERRORS     = 1us
    | SEM_NOALIGNMENTFAULTEXCEPT = 4us
    | SEM_NOGPFAULTERRORBOX      = 2us
    | SEM_NOOPENFILEERRORBOX     = 32768us


[<DllImport("kernel32.dll")>]
extern ErrorModes SetErrorMode(ErrorModes uMode);


let setWorkingDirectory (project: Project) =
    Directory.SetCurrentDirectory(project.Directories.SystemUnderTest)


let private loadFile (project: Project) (filename: string) : byte[] =
    let extension = (filename |> Path.GetExtension).ToLowerInvariant()
    if project.TextFileExtensions.Any(fun ext -> extension.Equals(ext, System.StringComparison.OrdinalIgnoreCase))
    then File.ReadAllText(filename) |> Convert.toBytes
    else File.ReadAllBytes(filename)


let loadExamples (project: Project) : TestCase list =
    Directory.EnumerateFiles(project.Directories.Examples)
        |> Seq.map (fun filename -> 
            { 
                Data          = loadFile project filename
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
