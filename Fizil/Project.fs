module Project


type Directories = {
    SystemUnderTest:  string
    Examples:         string
    Findings:         string
    ProjectDirectory: string
}


type Project = {
    Executable:         string
    Directories:        Directories
    TextFileExtensions: Set<string>
}


let private defaultDirectories (projectDirectory: string) =
    {
        SystemUnderTest  = System.IO.Path.Combine(projectDirectory, "system-under-test")
        Examples         = System.IO.Path.Combine(projectDirectory, "examples")
        Findings         = System.IO.Path.Combine(projectDirectory, "findings")
        ProjectDirectory = projectDirectory
    }


let private defaultProject (projectDirectory: string) =
    {
        Executable         = "TinyTest.exe"
        Directories        = defaultDirectories projectDirectory
        TextFileExtensions = [ ".txt" ] |> List.map (fun s -> s.ToLowerInvariant()) |> set
    }


let private forceDirectory (root: string) (directory: string) =
    let directory = System.IO.Path.Combine(root, directory)
    System.IO.Directory.CreateDirectory(directory) |> ignore


let load (path: string) =
    let projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.Environment.CurrentDirectory, "..\..\..\Demo"))        
    defaultProject projectDirectory


let initialize (project: Project) =
    let force = forceDirectory project.Directories.ProjectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Examples
    force project.Directories.Findings