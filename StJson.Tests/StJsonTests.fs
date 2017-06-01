namespace StJson.Tests

open System.IO
open System.Reflection
open NUnit.Framework
open FsUnitTyped
open StJson

module StJsonTests =

    type ParseResult =
    | Parsed        of JsonValue
    | Threw         of System.Exception
    | CouldNotParse of string

    let parseData(data: byte[]) : ParseResult =        
        let p = StJsonParser(data)
        try 
            match p.parse() with
            | Success jv          -> Parsed jv
            | SyntaxError message -> CouldNotParse message
        with
        | ex -> 
            Threw ex
    
    
    let parseString(s: string) : ParseResult =
        let data = System.Text.UTF8Encoding.UTF8.GetBytes s
        parseData(data)
        

    let testCase(jsonFilePath: string) : obj =
        let data = File.ReadAllBytes(jsonFilePath)
        let fileName = Path.GetFileName jsonFilePath
        let shouldParse =
            match fileName.StartsWith("y_"), fileName.StartsWith("n_") with
            | true, _      -> Some true
            | false, true  -> Some false
            | _            -> None
        [| data :> obj; fileName :> obj; shouldParse :> obj |] :> obj


    let testCases() : obj[] =
        let binDir = 
            typeof<StJsonParser>.Assembly.CodeBase
            |> (fun path -> if path.StartsWith("file:") then System.Uri(path).LocalPath else path)
            |> Path.GetDirectoryName
        let jsonTestSuiteDir = Path.Combine(binDir, "..", "..", "..", "JSONTestSuite")
        let jsonFiles = Directory.GetFiles(jsonTestSuiteDir, "*.json", SearchOption.AllDirectories)
        jsonFiles |> Array.map testCase
        
    
    [<Test>]
    [<TestCaseSource("testCases")>]
    let testFile(data: byte[], fileName: string, shouldParse: bool option) =
        let pr = parseData(data)
        match pr, shouldParse with
        | Threw ex,              Some true  -> Assert.Fail(sprintf "SHOULD_HAVE_PASSED\t%s, got %s" fileName ex.Message)
        | CouldNotParse message, Some true  -> Assert.Fail(sprintf "SHOULD_HAVE_PASSED\t%s, got %s" fileName message)
        | Parsed _,              Some false -> Assert.Fail(sprintf "SHOULD_HAVE_FAILED\t\%s" fileName)
        | _                                 -> ()


    [<Test>]    
    let testReadDouble() =
        let actual = parseString "12.34"
        actual |> shouldEqual (Parsed (JsonNumber "12.34")) 
    


    [<Test>]    
    let testReadDoubleWithExponent() =
        let actual = parseString "10.0e1"
        actual |> shouldEqual (Parsed (JsonNumber "10.0e1")) 


    [<Test>]
    let stringOverloadShouldGiveSameResultsAsBytes() = 
        let json  = """ { "abc": "def"} """
        let bytes = System.Text.Encoding.UTF8.GetBytes json

        let parsedAsBytes = StJsonParser(bytes).parse()
        let parsedAsString = StJsonParser(json).parse()

        parsedAsBytes |> shouldEqual parsedAsString
