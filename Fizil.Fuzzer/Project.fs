module Project

open FSharp.Configuration
open Log
open System.IO


type Project = YamlConfig<"project.yaml">

type DumbDirectories = {
    Examples: string
    Instrumented: string
    Out: string
    SystemUnderTest: string
}

type DumbExecute = {
    Executable: string
    Input: string
    Isolation: string
}

type DumbInstrument = {
    Exclude: string seq
    Include: string seq
}

type DumbProject = {
    Dictionaries: string seq
    Directories: DumbDirectories
    Execute: DumbExecute
    Instrument: DumbInstrument
}

let private absoluteProjectDirectory (projectPathAndFilename: string) : string = 
        Path.Combine(System.Environment.CurrentDirectory, projectPathAndFilename)
        |> Path.GetFullPath
        |> Path.GetDirectoryName

 
let private makeDirectoriesAbsolute (project: DumbProject) (projectPathAndFilename: string) : DumbProject =
    let projectDirectory = projectPathAndFilename |> absoluteProjectDirectory
    let toAbsolutePath directory = 
        Path.Combine(projectDirectory, directory) 
        |> Path.GetFullPath
    { project with
        Dictionaries = 
            project.Dictionaries 
            |> Seq.map toAbsolutePath
            |> fun dicts -> new System.Collections.Generic.List<string>(dicts)
        Directories = 
            {
                SystemUnderTest = project.Directories.SystemUnderTest |> toAbsolutePath
                Instrumented    = project.Directories.Instrumented    |> toAbsolutePath
                Examples        = project.Directories.Examples        |> toAbsolutePath
                Out             = project.Directories.Out             |> toAbsolutePath
            }
    }


let private makeDirectoriesRelativeTo (project: Project) (projectPathAndFilename: string) : Project =
    let projectDirectory = projectPathAndFilename |> absoluteProjectDirectory
    let pathRelativeToProject = WinApi.getRelativePath projectDirectory
    project.Directories.SystemUnderTest <- project.Directories.SystemUnderTest |> pathRelativeToProject
    project.Directories.Instrumented    <- project.Directories.Instrumented    |> pathRelativeToProject
    project.Directories.Examples        <- project.Directories.Examples        |> pathRelativeToProject
    project


let private defaultProject (projectDirectory: string) : DumbProject =
    let project = 
        {
            Dictionaries = []
            Execute = 
                { 
                    Executable = "TinyTest.exe"
                    Input = "OnCommandLine"
                    Isolation = "OutOfProcess"
                }
            Directories = 
                {
                    SystemUnderTest = "system-under-test"
                    Instrumented    = "instrumented"
                    Examples        = "examples"
                    Out             = "out"
                }
            Instrument = 
                {
                    Exclude = []
                    Include = [ "*.exe"; "*.dll" ] 
                }
        }
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


let private toDumbProject (project: Project) : DumbProject =
    {
        Dictionaries = project.Dictionaries
        Directories = 
            {
                Examples        = project.Directories.Examples
                Instrumented    = project.Directories.Instrumented
                Out             = project.Directories.Out
                SystemUnderTest = project.Directories.SystemUnderTest
            }
        Execute = 
            {
                Executable = project.Execute.Executable
                Input = project.Execute.Input
                Isolation = project.Execute.Isolation
            }
        Instrument = 
            {
                Exclude = project.Instrument.Exclude
                Include = project.Instrument.Include
            }
    }

let private toProject (project: DumbProject) : Project =
    let project = Project()
    project.Dictionaries <- project.Dictionaries
    project.Directories.Examples <- project.Directories.Examples
    project.Directories.Instrumented <- project.Directories.Instrumented
    project.Directories.SystemUnderTest <- project.Directories.SystemUnderTest
    project.Execute.Executable <- project.Execute.Executable
    project.Execute.Input <- project.Execute.Input
    project.Execute.Isolation <- project.Execute.Isolation
    project

let load (pathAndFilename: string) : DumbProject option =
    match File.Exists pathAndFilename with
    | true ->
        let project = Project()
        project.Load pathAndFilename
        Some (makeDirectoriesAbsolute (project |> toDumbProject) pathAndFilename)
    | false ->
        None 


let save (project: Project) (pathAndFilename: string) : unit = 
    makeDirectoriesRelativeTo project pathAndFilename |> ignore
    project.Save pathAndFilename


let private loadProjectOrDefault (log: Logger) (path: string) : DumbProject =
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
    project|> toProject |> save <| (projectFilename projectDirectory)