﻿open Arguments
open Log
open Project
open TestCase


let private disableErrorReporting() =
    // Disable error reporting for this process. 
    // That's inherited by child processes, so we don't get slowed by crash reporting.
    // See https://blogs.msdn.microsoft.com/oldnewthing/20160204-00/?p=92972
    Execute.SetErrorMode(Execute.ErrorModes.SEM_NOGPFAULTERRORBOX) |> ignore    


let private executeApplication (log: Logger) (project: Project) =
    match Execute.loadExamples project with
    | [] ->
        Log.error (sprintf "No example files found in %s" project.Directories.Examples)
        ExitCodes.examplesNotFound
    | examples ->
        let fullPath = System.IO.Path.Combine(project.Directories.SystemUnderTest, project.Executable)
        disableErrorReporting()
        log Verbose (sprintf "About to start %s" (System.IO.Path.GetFullPath fullPath))
        for example in examples |> Fuzz.all do
            log Verbose (sprintf "Test Case: %s" (example.Data |> Convert.toString))
            let result = Execute.executeApplication project example
            if (result.Crashed)
            then log Standard "Process crashed!"
            log Verbose (sprintf "StdOut: %s"    result.StdOut)
            log Verbose (sprintf "StdErr: %s"    result.StdErr)
            log Verbose (sprintf "Exit code: %i" result.ExitCode)
        log Standard "Execution complete"
        ExitCodes.success


let private printOptions (log: Logger) (arguments: Arguments) (project: Project) =
    log Verbose (sprintf "Current directory is %s" System.Environment.CurrentDirectory)
    log Verbose (sprintf "Project directory is %s" project.Directories.ProjectDirectory)


let private executeTests (log: Logger) (arguments: Arguments) (project: Project) =
    Execute.setWorkingDirectory project
    printOptions                log arguments project
    executeApplication          log project


let private reportVersion() =
    printfn "%A" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)


let private showHelp (options: Arguments) =
    printfn "%s" (Arguments.helpString options)


[<EntryPoint>]
let main argv = 
    try
        let arguments = Arguments.parse argv
        let log       = Log.create arguments.Verbosity
        let project   = Project.load("")
        let exitCode =
            match arguments.Operation with
            | Initialize    
                -> project |> Project.initialize
                   ExitCodes.success
            | ExecuteTests  
                -> executeTests log arguments project
            | ReportVersion 
                -> reportVersion()
                   ExitCodes.success
            | ShowHelp      
                -> showHelp arguments
                   ExitCodes.success
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        exitCode
    with 
        |  ex ->
            Log.error ex.Message
            if (System.Diagnostics.Debugger.IsAttached)
            then System.Console.ReadLine() |> ignore
            ExitCodes.internalError
