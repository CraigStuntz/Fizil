module Instrument

open Log
open Project
open System.IO
open System.Linq


let private fizilInstrumentAssemblyName = typeof<Fizil.Instrumentation.Instrument>.Assembly.Location |> Path.GetFileName

let private filesToInstrument (systemUnderTestFiles: Set<string>, project: Project) : Set<string> =
    let excluded = 
        project.Instrument.Exclude 
        |> Set.ofSeq
        |> Set.add fizilInstrumentAssemblyName
    systemUnderTestFiles
        |> Set.add project.Executable
        |> (fun files -> Set.difference files excluded)


let systemUnderTestFiles(project: Project) : Set<string> = 
    project.Instrument.Include
        |> Seq.collect (fun pattern -> Directory.GetFiles(project.Directories.SystemUnderTest, pattern))
        |> Seq.map Path.GetFileName
        |> Set.ofSeq


let project (project: Project, log: Logger) =
    log Standard "Starting instrumentation..."
    let files = systemUnderTestFiles project
    let instrument = filesToInstrument(files, project)
    let executableInputFilename = Path.Combine(project.Directories.SystemUnderTest, project.Executable)
    let executableOutputFilename = Path.Combine(project.Directories.Instrumented, project.Executable)
    let instrumentMethod = CilInstrument.instrumentExecutable(executableInputFilename, executableOutputFilename)
    instrument
        |> Set.remove project.Executable
        |> Set.iter (fun filename ->
            let inputFilename = Path.Combine(project.Directories.SystemUnderTest, filename)
            let outputFilename = Path.Combine(project.Directories.Instrumented, filename)
            log Verbose (sprintf "  Instrumenting %s" filename)
            CilInstrument.instrumentDependency(inputFilename, outputFilename, instrumentMethod))
    log Standard "Copying dependencies..."
    let copy = 
        files
        |> (fun allFiles -> Set.difference allFiles instrument)
        |> List.ofSeq
        |> List.map    (fun filename -> 
            Path.Combine(project.Directories.SystemUnderTest, filename),
            Path.Combine(project.Directories.Instrumented, filename))
        |> List.filter (fun (existing, instrumented) -> File.Exists(existing))
        
    copy |> Seq.iter (fun (existing, instrumented) -> 
        log Verbose (sprintf "  Copying %s" (Path.GetFileName existing))
        File.Copy(existing, instrumented, true))
    log Standard "Instrumentation complete"

    