open Log
open Options
open TestCase

let executeApplication(options: Options) =
    let testCase = {
        Arguments     = Some "abc"
        InputFileName = None
        StdIn         = None
    }
    Execute.executeApplication options testCase

let private forceDirectory (root: string) (directory: string) =
    let directory = System.IO.Path.Combine(root, directory)
    System.IO.Directory.CreateDirectory(directory) |> ignore

let printOptions (options: Options) =
    log options.Verbosity Verbose (sprintf "Current directory is %s" System.Environment.CurrentDirectory)
    log options.Verbosity Verbose (sprintf "Project directory is %s" options.Directories.ProjectDirectory)
    let fullPath = System.IO.Path.Combine([|options.Directories.ProjectDirectory; options.Directories.SystemUnderTest; options.Application.Executable |])
    log options.Verbosity Verbose (sprintf "About to start %s" (System.IO.Path.GetFullPath fullPath))
    
let private initialize (options: Options) =
    let force = forceDirectory options.Directories.ProjectDirectory
    force options.Directories.SystemUnderTest
    force options.Directories.Examples
    force options.Directories.Findings

let private executeTests (options: Options) =
    printOptions options
    let result = executeApplication options
    printfn "StdOut: %s" result.StdOut
    printfn "StdErr: %s" result.StdErr
    0

let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)

let private showHelp (options: Options) =
    printfn "%s" (Options.helpString options)

[<EntryPoint>]
let main argv = 
    try
        let options = Options.parse argv
        let exitCode =
            match options.Operation with
            | Initialize    -> initialize options; 0
            | ExecuteTests  -> executeTests options
            | ReportVersion -> reportVersion();    0
            | ShowHelp      -> showHelp options;   0
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        exitCode
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            if (System.Diagnostics.Debugger.IsAttached)
            then System.Console.ReadLine() |> ignore
            1
