open Newtonsoft.Json
open System.Collections.Generic
open Fizil.Instrumentation

type Result<'a> = 
    | Success of 'a
    | Error of string

let readAllFromStdIn() : string =
    let result = new System.Text.StringBuilder()
    let rec loop = function
        | null -> result.ToString()
        | s    -> result.AppendLine(s) |> ignore; loop (System.Console.ReadLine())
    loop (System.Console.ReadLine())
    

let parseJson (stdin: string) : Result<Dictionary<System.String, obj>> =
    try JsonConvert.DeserializeObject<Dictionary<System.String, obj>>(stdin) |> Success
    with 
    | :? JsonReaderException        as jre -> jre.Message |> Error
    | :? JsonSerializationException as jse -> jse.Message |> Error
    | :? System.FormatException     as jse -> 
        if jse.Message.StartsWith("Invalid hex character") // hard coded: https://github.com/JamesNK/Newtonsoft.Json/blob/6d7c94e69fa2f52b91fb22972321cb9b51b9abed/Src/Newtonsoft.Json/Utilities/ConvertUtils.cs#L984
        then jse.Message |> Error
        else reraise()


let stringify (ob: obj) : string =
    JsonConvert.SerializeObject(ob)


let private testJsonNet (maybeJson: string) : TestResult =
    match maybeJson with 
    | "" -> TestResult(false, 1, (sprintf "Expected JSON; found %A" maybeJson), "")
    | _ ->
        try
        match maybeJson |> parseJson with
            | Success dictionary ->
                dictionary 
                    |> stringify
                    |> ignore
                TestResult(false, 0, "", "")
            | Error message -> TestResult(false, 1, (sprintf "Error parsing JSON: %s" message), "")
        with
        | ex -> TestResult.UnhandledException(ex.Message)


[<FizilEntryPoint>]
let public test (maybeJson: string) : TestResult =
    testJsonNet maybeJson


[<EntryPoint>]
let main argv = 
    use stdIn = System.Console.OpenStandardInput()
    use streamReader = new System.IO.StreamReader(stdIn)
    let json = streamReader.ReadToEnd()
    let result = testJsonNet json
    if not (System.String.IsNullOrEmpty result.StdErr)
    then eprintfn "%s" result.StdErr
    if not (System.String.IsNullOrEmpty result.StdOut)
    then printfn "%s" result.StdOut
    result.ExitCode

