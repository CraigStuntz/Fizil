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
        System.Console.ForegroundColor <- System.ConsoleColor.White
        printfn "Press any key to exit" 
        System.Console.ReadKey() |> ignore


[<EntryPoint>]
let main argv = 
    try
        System.Console.BufferHeight <- int(System.Int16.MaxValue) - 1
        let arguments        = Arguments.parse argv
        let exitCode =
            match arguments.Operation with
            | Initialize -> 
                let log              = Log.create(Some System.Console.Out, arguments.Verbosity)
                let projectDirectory = System.IO.Path.GetDirectoryName arguments.ProjectFileName                
                Project.initialize log projectDirectory
                ExitCodes.success
            | Instrument -> 
                let log              = Log.create(Some System.Console.Out, arguments.Verbosity)
                match Project.load arguments.ProjectFileName with
                | Some project -> 
                    Instrument.project(project, log)
                    ExitCodes.success
                | None -> 
                    Log.error (sprintf "Project file %s not found" arguments.ProjectFileName)
                    ExitCodes.projectFileNotFound
            | ExecuteTests -> 
                let log              = Log.create(None, arguments.Verbosity)
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
