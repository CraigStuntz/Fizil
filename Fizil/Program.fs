open Log
open Options
open TestCase

let executeApplication(options: Options) =
    let testCase = {
        Arguments = Some "abc"
        StdIn = None
    }
    Execute.executeApplication options testCase

let printOptions (options: Options) =
    log options.Verbosity Verbose (sprintf "Working directory is %s" options.Directories.WorkingDirectory)
    let fullPath = System.IO.Path.Combine([|options.Directories.WorkingDirectory; options.Application.Executable |])
    log options.Verbosity Verbose (sprintf "About to start %s" fullPath)
    
let private initialize (options: Options) =
    0

let private executeTests (options: Options) =
    printOptions options
    let result = executeApplication options
    printfn "StdOut: %s" result.StdOut
    printfn "StdErr: %s" result.StdErr
    if (System.Diagnostics.Debugger.IsAttached)
    then System.Console.ReadLine() |> ignore
    0

let private reportVersion =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)
    0

let private showHelp (options: Options) =
    0

[<EntryPoint>]
let main argv = 
    try
        let options = Options.parse argv
        match options.Operation with
        | Initialize    -> initialize options
        | ExecuteTests  -> executeTests options
        | ReportVersion -> reportVersion
        | ShowHelp      -> showHelp options
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            1
