module Project


type Directories = {
    SystemUnderTest:  string
    Examples:         string
    Findings:         string
    ProjectDirectory: string
}


type Project = {
    Executable: string
    Directories: Directories
}


let private defaultDirectories =
    {
        SystemUnderTest  = "system-under-test"
        Examples         = "examples"
        Findings         = "findings"
        ProjectDirectory = "..\..\..\Demo" 
    }


let defaultProject = 
    {
        Executable       = "TinyTest.exe"
        Directories      = defaultDirectories
    }