open Newtonsoft.Json
open System.Collections.Generic


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

[<EntryPoint>]
let main argv = 
    use stdIn = System.Console.OpenStandardInput()
    use streamReader = new System.IO.StreamReader(stdIn)
    let json = streamReader.ReadToEnd()
    match json with 
    | "" -> 
        eprintfn "Expected JSON; found %A" argv
        1 // error if argv count <> 1
    | _ ->
        match json |> parseJson with
        | Success dictionary ->
            dictionary 
                |> stringify
                |> ignore
            0 // return an integer exit code
        | Error message ->
            eprintfn "Error parsing JSON: %s" message
            1

