open Arguments
open Log
open Project
open TestCase

let executeApplication(project: Project) =
    let testCase = {
        Arguments     = Some "abc"
        InputFileName = None
        StdIn         = None
    }
    Execute.executeApplication project testCase

let private forceDirectory (root: string) (directory: string) =
    let directory = System.IO.Path.Combine(root, directory)
    System.IO.Directory.CreateDirectory(directory) |> ignore

let printOptions (arguments: Arguments) (project: Project) =
    log arguments.Verbosity Verbose (sprintf "Current directory is %s" System.Environment.CurrentDirectory)
    log arguments.Verbosity Verbose (sprintf "Project directory is %s" project.Directories.ProjectDirectory)
    let fullPath = System.IO.Path.Combine([|project.Directories.ProjectDirectory; project.Directories.SystemUnderTest; project.Executable |])
    log arguments.Verbosity Verbose (sprintf "About to start %s" (System.IO.Path.GetFullPath fullPath))
    
let private initialize (project: Project) =
    let force = forceDirectory project.Directories.ProjectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Examples
    force project.Directories.Findings

let private executeTests (arguments: Arguments) (project: Project) =
    printOptions arguments project
    let result = executeApplication project
    printfn "StdOut: %s" result.StdOut
    printfn "StdErr: %s" result.StdErr
    0

let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)

let private showHelp (options: Arguments) =
    printfn "%s" (Arguments.helpString options)

[<EntryPoint>]
let main argv = 
    try
        let arguments = Arguments.parse argv
        let project = Project.defaultProject
        let exitCode =
            match arguments.Operation with
            | Initialize    -> initialize project; 0
            | ExecuteTests  -> executeTests arguments project
            | ReportVersion -> reportVersion();    0
            | ShowHelp      -> showHelp arguments;   0
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        exitCode
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            if (System.Diagnostics.Debugger.IsAttached)
            then System.Console.ReadLine() |> ignore
            1
