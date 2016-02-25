open Arguments
open Log
open Project
open TestCase


let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)


let private showHelp (options: Arguments) =
    printfn "%s" (Arguments.helpString options)


let private waitIfDebugging() =
    if (System.Diagnostics.Debugger.IsAttached)
    then
        printfn "Press any key to exit" 
        System.Console.ReadKey() |> ignore


[<EntryPoint>]
let main argv = 
    try
        let arguments        = Arguments.parse argv
        let log              = Log.create arguments.Verbosity
        let projectDirectory = System.IO.Path.GetDirectoryName arguments.ProjectFileName
        let exitCode =
            match arguments.Operation with
            | Initialize -> 
                Project.initialize log projectDirectory
                ExitCodes.success
            | ExecuteTests -> 
                match Project.load arguments.ProjectFileName with
                | Some project -> 
                    Execute.allTests log project
                | None -> 
                    Log.error (sprintf "Project file %s not found" arguments.ProjectFileName)
                    ExitCodes.projectFileNotFound
            | ReportVersion 
                -> reportVersion()
                   ExitCodes.success
            | ShowHelp      
                -> showHelp arguments
                   ExitCodes.success
        waitIfDebugging()
        exitCode
    with 
        |  ex ->
            Log.error ex.Message
            waitIfDebugging()
            ExitCodes.internalError
