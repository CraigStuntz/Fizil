module Arguments

open Argu
open Log
open Project

type InputSource = 
    | OnCommandLine
    | OnStandardInput

type Isolation = 
    | InProcessSerial
    | OutOfProcess

type CliArgument = 
    // Operations
    | Init
    | Instrument
    | Fuzz
    | Version
    // Directories
    | [<AltCommandLine("-i"); Unique>] In of dir: string
    | [<AltCommandLine("-o"); Unique>] Out of dir: string
    | System_Under_Test of dir: string
    | Instrumented of dir: string
    // Executable
    | [<Unique>] Executable of string
    | [<Unique>] Input of InputSource
    | [<Unique>] Isolation of Isolation
    // Instrumentation
    | Instrument_Include of files: string list
    | Instrument_Exclude of files: string list
    // Verbosity
    | Quiet
    | Verbose
    // Misc
    | [<AltCommandLine("-x")>] Dictionary of string
    // Project
    | [<MainCommand; ExactlyOnce; Last>] ProjectName of project:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            // Operations
            | Init                 -> "Create a default project file"
            | Instrument           -> "Instrument binaries in system under test folder"
            | Fuzz                 -> "Execute tests by fuzzing examples"
            | Version              -> "Report the application version"
            // Directories
            | In _                 -> "Input directory with test cases"
            | Out _                -> "Output directory for fuzzer findings (NB: unimplemented!)"
            | System_Under_Test _  -> "Directory containing assemblies to instrument"
            | Instrumented _       -> "Directory in which to place instrumented assemblies"
            // Executable
            | Executable _         -> "Entry point for fuzzing"
            | Input _              -> "Method of passing input to system under test"
            | Isolation _          -> "Run out of process in parallel or in process serial"
            // Instrumentation
            | Instrument_Include _ -> "Filename(s) to instrument. Wildcards permitted"
            | Instrument_Exclude _ -> "Filename(s) to exclude from instrumentation. No wildcards"
            // Verbosity
            | Quiet                -> "Suppress all non-error command line output"
            | Verbose              -> "Display additional status information on command line output"
            // Misc 
            | Dictionary _         -> "Optional fuzzer dictionary"
            // Project
            | ProjectName _        -> "Project filename"


type Operation = 
    | Initialize
    | Instrument
    | ExecuteTests
    | ReportVersion
    | ShowHelp

type Instrument = {
    Include: string list
    Exclude: string list
 }

type Arguments = {
    DictionaryFileName: string option
    Directories:        DumbDirectories
    Instrument:         Instrument
    Operations:         Operation list
    ProjectFileName:    string
    Verbosity:          Verbosity
}


let defaultArguments(parseResults: ParseResults<CliArgument>) = 
    {
        DictionaryFileName = parseResults.TryGetResult(<@ Dictionary @>)
        Directories     = 
            {
                Examples        = parseResults.GetResult(<@ In @>,                 "examples")
                Out             = parseResults.GetResult(<@ Out @>,                "out")
                SystemUnderTest = parseResults.GetResult(<@ System_Under_Test @>,  "system-under-test")
                Instrumented    = parseResults.GetResult(<@ Instrumented @>,       "instrumented")
            }
        Instrument = 
            {
                Include = parseResults.GetResult(<@ Instrument_Include @>)
                Exclude = parseResults.GetResult(<@ Instrument_Exclude @>)
            }
        Operations      = []
        ProjectFileName = parseResults.GetResult(<@ ProjectName @>, "project.yaml")
        Verbosity       = Standard
    }


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