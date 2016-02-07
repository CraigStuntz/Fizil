open Options
open TestCase

let executeApplication(start: Start) =
    let testCase = {
        Arguments = Some "abc"
        StdIn = None
    }
    Execute.executeApplication start testCase

let parseOptions (argv: string[]) =
    {
        Application = 
            {
                Executable       = "..\\..\\..\\TinyTest\\bin\\Debug\\TinyTest.exe"
                WorkingDirectory = System.Environment.CurrentDirectory
            }
        Verbosity = Verbose
    }

let printOptions (options: Options) =
    match options.Verbosity with
    | Verbose ->
        let fullPath = System.IO.Path.Combine([|options.Application.WorkingDirectory; options.Application.Executable |])
        printfn "About to start %s" fullPath
    | Standard 
        -> printfn "Verbose"
    | Quiet 
        -> ()

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    printfn "Launching..."
    printfn "Working directory is %s" System.Environment.CurrentDirectory
    try
        let options = parseOptions argv
        let result = executeApplication options.Application
        printfn "Returned %s" result
        if (System.Diagnostics.Debugger.IsAttached)
        then System.Console.ReadLine() |> ignore
        0
    with 
        |  ex ->
            eprintfn "Error: %s" ex.Message
            1
