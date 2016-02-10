module Execute

open System.Diagnostics
open System.IO
open ExecutionResult
open Project
open TestCase


let setWorkingDirectory (project: Project) =
    Directory.SetCurrentDirectory(project.Directories.SystemUnderTest)


let private loadFile (project: Project) (filename: string) : byte[] =
    let extension = (filename |> Path.GetExtension).ToLowerInvariant()
    if project.TextFileExtensions |> Set.contains extension 
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
    proc.Close()
    {
        StdErr = err
        StdOut = output
    }
