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
    

let parseJson (bytes: byte[]) : ParseResult =
    let jsonString = bytes |> System.Text.Encoding.UTF8.GetString
    let jsonNetResult = 
        try JsonConvert.DeserializeObject<Dictionary<System.String, obj>>(jsonString) |> ignore
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
            let parser = StJson.StJsonParser(bytes |> List.ofArray, 500, StJson.Options.none)
            match parser.parse() with
                | Some _ -> Success
                | None   -> Error "No object returned from StJsonParser"
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
        match maybeJson |> parseJson with
            | { JsonNet = Success; StJson = Success } 
            | { JsonNet = Error _; StJson = Error _ } -> 
                TestResult(false, 0, "", "")
            | { JsonNet = Success; StJson = Error message } -> 
                TestResult(false, 1, (sprintf "JsonNet returned Success; StJson returned error: %s" message), "") 
            | { JsonNet = Error message; StJson = Success } -> 
                TestResult(false, 1, (sprintf "JsonNet returned Success; StJson returned error: %s" message), "") 
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
    use stdIn = System.Console.OpenStandardInput()
    use streamReader = new System.IO.BinaryReader(stdIn)
    let json = readBytesFromStream streamReader
    let result = compareParsers json
    if not (System.String.IsNullOrEmpty result.StdErr)
    then eprintfn "%s" result.StdErr
    if not (System.String.IsNullOrEmpty result.StdOut)
    then printfn "%s" result.StdOut
    result.ExitCode

