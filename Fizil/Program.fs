open Arguments
open Log
open Project


let executeApplication (log: Logger) (project: Project) =
    let seed = "abc" |> Convert.toBytes
    for example in [ seed ] |> Fuzz.all do
        let testCase = example |> Convert.toString
        log Verbose (sprintf "Test Case: %s" testCase)
        let result = Execute.executeApplication project testCase
        log Verbose (sprintf "StdOut: %s" result.StdOut)
        log Verbose (sprintf "StdErr: %s" result.StdErr)


let private forceDirectory (root: string) (directory: string) =
    let directory = System.IO.Path.Combine(root, directory)
    System.IO.Directory.CreateDirectory(directory) |> ignore


let printOptions (log: Logger) (arguments: Arguments) (project: Project) =
    log Verbose (sprintf "Current directory is %s" System.Environment.CurrentDirectory)
    log Verbose (sprintf "Project directory is %s" project.Directories.ProjectDirectory)
    let fullPath = System.IO.Path.Combine([|project.Directories.ProjectDirectory; project.Directories.SystemUnderTest; project.Executable |])
    log Verbose (sprintf "About to start %s" (System.IO.Path.GetFullPath fullPath))

    
let private initialize (project: Project) =
    let force = forceDirectory project.Directories.ProjectDirectory
    force project.Directories.SystemUnderTest
    force project.Directories.Examples
    force project.Directories.Findings


let private executeTests (log: Logger) (arguments: Arguments) (project: Project) =
    Execute.initialize project
    printOptions log arguments project
    let result = executeApplication log project
    0


let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)


let private showHelp (options: Arguments) =
    printfn "%s" (Arguments.helpString options)


[<EntryPoint>]
let main argv = 
    try
        let arguments = Arguments.parse argv
        let log       = Log.create arguments.Verbosity
        let project   = Project.defaultProject
        let exitCode =
            match arguments.Operation with
            | Initialize    -> initialize project; 0
            | ExecuteTests  -> executeTests log arguments project
            | ReportVersion -> reportVersion();    0
            | ShowHelp      -> showHelp arguments; 0
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        exitCode
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            if (System.Diagnostics.Debugger.IsAttached)
            then System.Console.ReadLine() |> ignore
            1
