module Project

open FSharp.Configuration
open Log
open System.IO


type Project = YamlConfig<"project.yaml">


let private absoluteProjectDirectory (projectPathAndFilename: string) : string = 
        Path.Combine(System.Environment.CurrentDirectory, projectPathAndFilename)
        |> Path.GetFullPath
        |> Path.GetDirectoryName


let private makeDirectoriesAbsolute (project: Project) (projectPathAndFilename: string) : Project =
    let projectDirectory = projectPathAndFilename |> absoluteProjectDirectory
    let toAbsolutePath directory = 
        Path.Combine(projectDirectory, directory) 
        |> Path.GetFullPath
    project.Directories.SystemUnderTest <- project.Directories.SystemUnderTest |> toAbsolutePath
    project.Directories.Instrumented    <- project.Directories.Instrumented    |> toAbsolutePath
    project.Directories.Examples        <- project.Directories.Examples        |> toAbsolutePath
    project


let private makeDirectoriesRelativeTo (project: Project) (projectPathAndFilename: string) : Project =
    let projectDirectory = projectPathAndFilename |> absoluteProjectDirectory
    let pathRelativeToProject = WinApi.getRelativePath projectDirectory
    project.Directories.SystemUnderTest <- project.Directories.SystemUnderTest |> pathRelativeToProject
    project.Directories.Instrumented    <- project.Directories.Instrumented    |> pathRelativeToProject
    project.Directories.Examples        <- project.Directories.Examples        |> pathRelativeToProject
    project


let private defaultProject (projectDirectory: string) : Project =
    let project = Project()
    project.Execute.Executable          <- "TinyTest.exe"
    project.Directories.SystemUnderTest <- "system-under-test"
    project.Directories.Instrumented    <- "instrumented"
    project.Directories.Examples        <- "examples"
    project.TextFileExtensions          <- System.Collections.Generic.List([ ".txt" ])
    makeDirectoriesAbsolute project projectDirectory


let private forceDirectory (log: Logger) (root: string) (directory: string) : unit =
    let directory = Path.Combine(root, directory)
    match Directory.Exists directory with
    | false ->
        log.ToFile Standard (sprintf "CREATE %s" directory)
        Directory.CreateDirectory(directory) |> ignore
    | true ->
        log.ToFile Standard (sprintf "USE %s" directory)


let private projectFilename (path: string) : string =
    if ".yaml".Equals(Path.GetExtension path, System.StringComparison.OrdinalIgnoreCase)
    then path
    else Path.Combine(path, "project.yaml")


let load (pathAndFilename: string) : Project option =
    match File.Exists pathAndFilename with
    | true ->
        let project = Project()
        project.Load pathAndFilename
        Some (makeDirectoriesAbsolute project pathAndFilename)
    | false ->
        None 


let save (project: Project) (pathAndFilename: string) : unit = 
    makeDirectoriesRelativeTo project pathAndFilename |> ignore
    project.Save pathAndFilename


let private loadProjectOrDefault (log: Logger) (path: string) : Project =
    let filename = projectFilename path
    match load filename with
    | Some project -> 
        log.ToFile Standard (sprintf "USE %s" filename)
        project
    | None         -> 
        log.ToFile Standard (sprintf "CREATE %s" filename)
        defaultProject path


let initialize (log: Logger) (projectDirectory: string) : unit =
    let project = loadProjectOrDefault log projectDirectory
    let force   = forceDirectory log projectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Instrumented
    force project.Directories.Examples
    project |> save <| (projectFilename projectDirectory)