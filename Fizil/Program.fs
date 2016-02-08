open Log
open Options
open TestCase

let executeApplication(start: Start) =
    let testCase = {
        Arguments = Some "abc"
        StdIn = None
    }
    Execute.executeApplication start testCase

let printOptions (options: Options) =
    let fullPath = System.IO.Path.Combine([|options.Application.WorkingDirectory; options.Application.Executable |])
    log options.Verbosity Verbose (sprintf "About to start %s" fullPath)

[<EntryPoint>]
let main argv = 
    printfn "Working directory is %s" System.Environment.CurrentDirectory
    try
        let options = Options.parse argv
        let result = executeApplication options.Application
        printfn "StdOut: %s" result.StdOut
        printfn "StdErr: %s" result.StdErr
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        0
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            1
