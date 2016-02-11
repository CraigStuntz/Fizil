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
        let projectFile      = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.Environment.CurrentDirectory, "..\..\..\Demo\project.yaml"))
        let projectDirectory = System.IO.Path.GetDirectoryName projectFile
        let project          = Project.load projectFile
        let exitCode =
            match arguments.Operation with
            | Initialize    
                -> Project.initialize project projectDirectory
                   ExitCodes.success
            | ExecuteTests  
                -> Execute.allTests log project
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
