module Arguments

open Argu
open Log

type CliArgument = 
    | Init
    | Instrument
    | Fuzz
    | [<MainCommand; ExactlyOnce; Last>] ProjectName of project:string
    | Version
    | Quiet
    | Verbose
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Init          -> "Create a default project file"
            | Instrument    -> "Instrument binaries in system under test folder"
            | Fuzz          -> "Execute tests by fuzzing examples"
            | ProjectName _ -> "Project filename"
            | Version       -> "Report the application version"
            | Quiet         -> "Suppress all non-error command line output"
            | Verbose       -> "Display additional status information on command line output"


type Operation = 
    | Initialize
    | Instrument
    | ExecuteTests
    | ReportVersion
    | ShowHelp

type Arguments = {
    Operations:      Operation list
    ProjectFileName: string
    Verbosity:       Verbosity
}


let defaultArguments(parseResults: ParseResults<CliArgument>) = 
    {
        Operations       = []
        ProjectFileName = parseResults.GetResult(<@ ProjectName @>, "project.yaml")
        Verbosity       = Standard
    }


let private (|ProjectFile|_|) str = 
    if (System.IO.File.Exists str)
    then Some str
    else None

let parser = ArgumentParser.Create<CliArgument>(programName = "fizil.exe")
let usage = parser.PrintUsage()

let private parseOperation (parseResults: ParseResults<CliArgument>) (accum: Arguments) (cliArgument, operation) : Arguments =
    if parseResults.Contains cliArgument 
    then { accum with Operations = accum.Operations @ [ operation ] }
    else accum

let private parseOperations (parseResults: ParseResults<CliArgument>) (accum: Arguments) : Arguments = 
    let cliArgumentToOperationMap = [
        (<@ Init @>,                   Initialize)
        (<@ CliArgument.Instrument @>, Operation.Instrument)
        (<@ Fuzz @>,                   ExecuteTests)
        (<@ Version @>,                ReportVersion)
    ]
    List.fold (parseOperation parseResults) accum cliArgumentToOperationMap

let private parseVerbosity (parseResults: ParseResults<CliArgument>) (accum: Arguments) : Arguments =
    if parseResults.Contains <@ Verbose @> 
    then { accum with Verbosity = Verbosity.Verbose }
    else 
        if parseResults.Contains <@ Quiet @>
        then { accum with Verbosity = Verbosity.Quiet }
        else accum


let parse (argv: string[]) = 
    let parseResults = parser.Parse argv
    (defaultArguments parseResults)
        |> parseOperations parseResults
        |> parseVerbosity parseResults