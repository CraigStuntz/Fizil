open Arguments
open Log
open Project
open System
open TestCase


let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)


let private showHelp () =
    printfn "%s" (Arguments.helpString())


let private waitIfDebugging() =
    if (System.Diagnostics.Debugger.IsAttached)
    then
        Console.ForegroundColor <- System.ConsoleColor.White
        Console.CursorVisible <- true
        printfn "Press any key to exit" 
        Console.ReadKey() |> ignore


let private executeOperation(operation: Operation, arguments: Arguments) : int =
    match operation with
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
            Console.BackgroundColor <- Display.backgroundColor
            Console.Clear()
            Execute.allTests log project
        | None -> 
            Log.error (sprintf "Project file %s not found" arguments.ProjectFileName)
            ExitCodes.projectFileNotFound
    | ReportVersion-> 
        reportVersion()
        ExitCodes.success
    | ShowHelp -> 
        showHelp()
        ExitCodes.success


/// Exit multiple operations. Fail immediately if any return non-success
let private executeOperations(arguments: Arguments) : int =
    let folder = fun exitCode operation -> 
        match exitCode with
        | ExitCodes.success -> executeOperation(operation, arguments)
        | _ -> exitCode
    List.fold folder ExitCodes.success arguments.Operations


[<EntryPoint>]
let main argv = 
    let originalForegroundColor = Console.ForegroundColor
    let originalBackgroundColor = Console.BackgroundColor
    try
        try
            Console.CursorVisible <- false        
            Console.BufferHeight <- int(System.Int16.MaxValue) - 1
            let arguments        = Arguments.parse argv
            let exitCode = executeOperations arguments
            waitIfDebugging()
            exitCode
        with 
            |  :? System.AggregateException as aggEx ->  
                aggEx.InnerExceptions 
                    |> Seq.map (fun e -> e.Message) 
                    |> Seq.distinct
                    |> (String.concat System.Environment.NewLine)
                    |> Log.error
                waitIfDebugging()
                ExitCodes.internalError
            |  ex ->
                Log.error ex.Message
                waitIfDebugging()
                ExitCodes.internalError
    finally
        Console.CursorVisible   <- true
        Console.ForegroundColor <- originalForegroundColor
        Console.BackgroundColor <- originalBackgroundColor
