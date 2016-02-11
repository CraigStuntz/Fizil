module Project

open FSharp.Configuration


type Project = YamlConfig<"project.yaml">


let private defaultProject (projectDirectory: string) =
    let project = Project()
    project.Executable                  <- "TinyTest.exe"
    project.Directories.SystemUnderTest <- System.IO.Path.Combine(projectDirectory, "system-under-test")
    project.Directories.Examples        <- System.IO.Path.Combine(projectDirectory, "examples")
    project.Directories.Findings        <- System.IO.Path.Combine(projectDirectory, "findings")
    project.TextFileExtensions          <- System.Collections.Generic.List([ ".txt" ])
    project


let private forceDirectory (root: string) (directory: string) =
    let directory = System.IO.Path.Combine(root, directory)
    System.IO.Directory.CreateDirectory(directory) |> ignore


let load (path: string) =
    let projectDirectory = System.IO.Path.GetDirectoryName(path);        
    defaultProject projectDirectory


let initialize (project: Project) (projectDirectory: string) =
    let force = forceDirectory projectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Examples
    force project.Directories.Findings