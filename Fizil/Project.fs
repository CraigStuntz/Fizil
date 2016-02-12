module Project

open FSharp.Configuration
open Log
open System.IO


type Project = YamlConfig<"project.yaml">


let private makeDirectoriesAbsolute (project: Project) (projectPathAndFilename: string) =
    let projectDirectory = Path.GetDirectoryName(projectPathAndFilename)
    project.Directories.SystemUnderTest <- Path.Combine(projectDirectory, project.Directories.SystemUnderTest)
    project.Directories.Examples        <- Path.Combine(projectDirectory, project.Directories.Examples)
    project.Directories.Findings        <- Path.Combine(projectDirectory, project.Directories.Findings)
    project


let private makeDirectoriesRelativeTo (project: Project) (projectPathAndFilename: string) =
    let projectDirectory = Path.GetDirectoryName(projectPathAndFilename)
    let pathRelativeToProject = WinApi.getRelativePath projectDirectory
    project.Directories.SystemUnderTest <- project.Directories.SystemUnderTest |> pathRelativeToProject
    project.Directories.Examples        <- project.Directories.Examples        |> pathRelativeToProject
    project.Directories.Findings        <- project.Directories.Findings        |> pathRelativeToProject
    project


let private defaultProject (projectDirectory: string) =
    let project = Project()
    project.Executable                  <- "TinyTest.exe"
    project.Directories.SystemUnderTest <- "system-under-test"
    project.Directories.Examples        <- "examples"
    project.Directories.Findings        <- "findings"
    project.TextFileExtensions          <- System.Collections.Generic.List([ ".txt" ])
    makeDirectoriesAbsolute project projectDirectory


let private forceDirectory (log: Logger) (root: string) (directory: string) =
    let directory = Path.Combine(root, directory)
    match Directory.Exists directory with
    | false ->
        log Standard (sprintf "CREATE %s" directory)
        Directory.CreateDirectory(directory) |> ignore
    | true ->
        log Standard (sprintf "USE %s" directory)


let private projectFilename (path: string) =
    if ".yaml".Equals(Path.GetExtension path, System.StringComparison.OrdinalIgnoreCase)
    then path
    else Path.Combine(path, "project.yaml")


let load (pathAndFilename: string) =
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

let private loadProjectOrDefault (log: Logger) (path: string) =
    let filename = projectFilename path
    match load filename with
    | Some project -> 
        log Standard (sprintf "USE %s" filename)
        project
    | None         -> 
        log Standard (sprintf "CREATE %s" filename)
        defaultProject path


let initialize (log: Logger) (projectDirectory: string) =
    let project = loadProjectOrDefault log projectDirectory
    let force   = forceDirectory log projectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Examples
    force project.Directories.Findings
    project |> save <| (projectFilename projectDirectory)