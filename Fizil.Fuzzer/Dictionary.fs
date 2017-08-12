module Dictionary

open System.IO
open System.Text

let private backslashEscapedChar (value: char) : char =
    match value with
    | 'b' -> '\b'
    | 'f' -> '\f'
    | 'n' -> '\n'
    | 'r' -> '\r'
    | 't' -> '\t'
    | _   -> value

let rec private tokenize (value: char list) : char list=
    match value with 
    | []                -> failwith "Expected closing quote"
    | [ '"' ]           -> []
    | '\\' :: c :: rest -> backslashEscapedChar c :: tokenize rest
    | c :: rest         -> c :: tokenize rest

let private readValue (value: string) : string =
    if (value.StartsWith "\"") 
    then 
        let chars = value.Substring(1) |> Seq.toList |> tokenize
        System.String.Concat(Array.ofList chars)
    else
        failwith "Expected \""

let private readLine (line: string) : string option =
    let equalPos = line.IndexOf("=")
    match line with
    | _ when equalPos = -1 || equalPos = (line.Length - 1) 
        -> None
    | x when x.StartsWith("#")
        -> None
    | _ 
        -> Some (readValue (line.Substring(equalPos + 1)))

let readStrings (lines: string[]) =
    lines 
        |> Array.choose readLine

let private getEncoding(filename: string) : System.Text.Encoding =
    let bom : byte[] = Array.zeroCreate 4
    use file = new FileStream(filename, FileMode.Open, FileAccess.Read)
    file.Read(bom, 0, 4) |> ignore

    // Analyze the BOM
    match bom with 
    | [| 0x2buy; 0x2fuy; 0x76uy; _      |] -> Encoding.UTF7
    | [| 0xefuy; 0xbbuy; 0xbfuy; _      |] -> Encoding.UTF8
    | [| 0xffuy; 0xfeuy; _;      _      |] -> Encoding.Unicode; //UTF-16LE
    | [| 0xfeuy; 0xffuy; _;      _      |] -> Encoding.BigEndianUnicode; //UTF-16BE
    | [| 0uy;    0uy;    0xfeuy; 0xffuy |] -> Encoding.UTF32;
    | _                                    -> Encoding.ASCII;

/// Returns array of dictionary entries as byte arrays in same encoding
/// as orignal file
let readFile (filename: string) = 
    let encoding = getEncoding filename
    System.IO.File.ReadAllLines(filename)
    |> readStrings
    |> Array.map encoding.GetBytes

let readFiles (filenames: string seq) = 
    filenames 
    |> Array.ofSeq
    |> Array.collect readFile