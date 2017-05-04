open Newtonsoft.Json
open System.Collections.Generic
open Fizil.Instrumentation

type Result = 
    | Success
    | Error of string

type ParseResult = {
    JsonNet: Result
    StJson:  Result
}

let readAllFromStdIn() : string =
    let result = new System.Text.StringBuilder()
    let rec loop = function
        | null -> result.ToString()
        | s    -> result.AppendLine(s) |> ignore; loop (System.Console.ReadLine())
    loop (System.Console.ReadLine())
    

let parseJson (maybeJson: byte[]) : ParseResult =
    use stream = new System.IO.MemoryStream(maybeJson)
    use reader = new System.IO.StreamReader(stream, true)
    let jsonNetResult = 
        try JsonSerializer.Create().Deserialize(reader, typeof<obj>) |> ignore
            Success
        with 
        | :? JsonReaderException        as jre -> jre.Message |> Error
        | :? JsonSerializationException as jse -> jse.Message |> Error
        | :? System.FormatException     as fe -> 
            if fe.Message.StartsWith("Invalid hex character") // hard coded: https://github.com/JamesNK/Newtonsoft.Json/blob/6d7c94e69fa2f52b91fb22972321cb9b51b9abed/Src/Newtonsoft.Json/Utilities/ConvertUtils.cs#L984
            then fe.Message |> Error
            else reraise()
    let stJsonResult = 
        try
            let parser = StJson.StJsonParser(maybeJson |> List.ofArray, 500, StJson.Options.none)
            match parser.parse() with   
                | StJson.JsonParseResult.Success               _ -> Success
                | StJson.JsonParseResult.SyntaxError message   -> Error message
        with
        | ex -> ex.Message + "\r\n" + ex.StackTrace |> Error
    {
        JsonNet = jsonNetResult
        StJson  = stJsonResult
    }

let stringify (ob: obj) : string =
    JsonConvert.SerializeObject(ob)


let private compareParsers (maybeJson: byte[]) : TestResult =
    match maybeJson with 
    | [||] -> TestResult(false, 1, (sprintf "Expected JSON; found %A" maybeJson), "")
    | _ ->
        try
        let results = maybeJson |> parseJson
        if results.JsonNet = Success 
        then 
            match results.StJson with
            | Success -> TestResult(false, 0, "", "")
            | Error message -> TestResult(false, 1, (sprintf "JsonNet returned Success; StJson returned error: %s" message), "")
        else 
            if results.StJson = Success
            then
                match results.JsonNet with
                | Success -> TestResult(false, 0, "", "")
                | Error message -> TestResult(false, 1, (sprintf "StJson returned Success; JsonNet returned error: %s" message), "") 
            else TestResult(false, 0, "", "")
        with
        | ex -> TestResult.UnhandledException(ex.Message)


[<FizilEntryPoint>]
let public test (maybeJson: byte[]) : TestResult =
    compareParsers maybeJson


let rec private readBytesFromStream(stream: System.IO.BinaryReader) =
    let bytes = stream.ReadBytes 2048
    match bytes |> Array.length with
    | 2048 -> Array.concat(seq [ bytes; readBytesFromStream stream])
    | _    -> bytes


[<EntryPoint>]
let main argv = 
    let maybeJson = 
        if (argv |> Array.length) = 1
        then 
            System.IO.File.ReadAllBytes (argv |> Array.head)
        else
            use stdIn = System.Console.OpenStandardInput()
            use streamReader = new System.IO.BinaryReader(stdIn)
            readBytesFromStream streamReader
    let result = compareParsers maybeJson
    if not (System.String.IsNullOrEmpty result.StdErr)
    then eprintfn "%s" result.StdErr
    if not (System.String.IsNullOrEmpty result.StdOut)
    then printfn "%s" result.StdOut
    result.ExitCode

