module Instrument

open Log
open Project
open System.IO
open System.Linq


let private fizilInstrumentAssemblyName = typeof<Fizil.Instrumentation.Instrument>.Assembly.Location |> Path.GetFileName

let private filesToInstrument (systemUnderTestFiles: Set<string>, project: DumbProject) : Set<string> =
    let excluded = 
        project.Instrument.Exclude 
        |> Set.ofSeq
        |> Set.add fizilInstrumentAssemblyName
    systemUnderTestFiles
        |> Set.add project.Execute.Executable
        |> (fun files -> Set.difference files excluded)


let systemUnderTestFiles(project: DumbProject) : Set<string> = 
    project.Instrument.Include
        |> Seq.collect (fun pattern -> Directory.GetFiles(project.Directories.SystemUnderTest, pattern))
        |> Seq.map Path.GetFileName
        |> Set.ofSeq


let project (project: DumbProject, log: Logger) =
    log.ToFile Standard "Starting instrumentation..."
    let files = systemUnderTestFiles project
    let instrument = filesToInstrument(files, project)
    let instrumentAssemblies = Set.map (fun fileName -> System.IO.Path.GetFileNameWithoutExtension fileName) instrument
    log.ToFile Standard (sprintf "Instrumenting %s" project.Execute.Executable)
    let executableInputFilename = Path.Combine(project.Directories.SystemUnderTest, project.Execute.Executable)
    let executableOutputFilename = Path.Combine(project.Directories.Instrumented, project.Execute.Executable)
    CilInstrument.instrumentExecutable(executableInputFilename, instrumentAssemblies, executableOutputFilename)
    log.ToFile Standard "Instrumenting dependencies..."
    instrument
        |> Set.remove project.Execute.Executable
        |> Set.iter (fun filename ->
            let inputFilename = Path.Combine(project.Directories.SystemUnderTest, filename)
            let outputFilename = Path.Combine(project.Directories.Instrumented, filename)
            log.ToFile Verbose (sprintf "  Instrumenting %s" filename)
            CilInstrument.instrumentDependency(inputFilename, outputFilename))
    log.ToFile Standard "Copying excluded dependencies..."
    let copy = 
        files
        |> (fun allFiles -> Set.difference allFiles instrument)
        |> List.ofSeq
        |> List.map    (fun filename -> 
            Path.Combine(project.Directories.SystemUnderTest, filename),
            Path.Combine(project.Directories.Instrumented, filename))
        |> List.filter (fun (existing, _instrumented) -> File.Exists(existing))
        
    copy |> Seq.iter (fun (existing, instrumented) -> 
        log.ToFile Verbose (sprintf "  Copying %s" (Path.GetFileName existing))
        File.Copy(existing, instrumented, true))
    log.ToFile Standard "Instrumentation complete"

    