module Display

open ExecutionResult
open System
open System.Globalization
open System.Threading


// these global counters are incremented by functions running in parallel
let private crashes_          = ref 0L
let private executions_       = ref 0L
let private nonZeroExitCodes_ = ref 0L
let private paths_            = ref 0L

let reset() =
    crashes_          := 0L
    executions_       := 0L
    nonZeroExitCodes_ := 0L
    paths_            := 0L

type Status = 
    {
        StartTime:           DateTimeOffset
        ElapsedTime:         TimeSpan
        StageName:           string
        Executions:          int64
        Crashes:             int64
        NonZeroExitCodes:    int64
        Paths:               int64
        ExecutionsPerSecond: float
        LastCrash:           string option
    }
    with 
        member this.AddExecution(stageName: string, result: Result) =
            let elapsedTime = DateTimeOffset.UtcNow - this.StartTime
            let executions = Interlocked.Increment(executions_)
            let executionsPerSecond = 
                if (elapsedTime.TotalMilliseconds > 0.0) 
                then Convert.ToDouble(executions) / Convert.ToDouble(elapsedTime.TotalMilliseconds) * 1000.0
                else 0.0
            let crashes = 
                if result.Crashed
                then Interlocked.Increment(crashes_)
                else !crashes_
            let nonZeroExitCodes = 
                if result.ExitCode <> 0
                then Interlocked.Increment(nonZeroExitCodes_)
                else !nonZeroExitCodes_
            let paths = 
                if result.NewPathFound
                then Interlocked.Increment(paths_)
                else !paths_
            {
                StartTime           = this.StartTime
                ElapsedTime         = elapsedTime
                StageName           = stageName
                Executions          = executions
                Crashes             = crashes
                NonZeroExitCodes    = nonZeroExitCodes
                Paths               = paths
                ExecutionsPerSecond = executionsPerSecond
                LastCrash           = 
                    match result.Crashed, result.HasStdErrOutput with
                    | true, true  -> Some result.StdErr
                    | true, false -> Some result.StdOut
                    | false, _    -> this.LastCrash
            }


let initialState() = 
    {
        StartTime           = DateTimeOffset.UtcNow
        ElapsedTime         = TimeSpan.Zero
        StageName           = "initializing"
        Executions          = 0L
        Crashes             = 0L
        NonZeroExitCodes    = 0L
        Paths               = 0L
        ExecutionsPerSecond = 0.0
        LastCrash           = None
    }


let private writeValue(title: string) (titleWith: int) (formattedValue: string) =
    Console.ForegroundColor <- ConsoleColor.Gray
    Console.Write ((title.PadLeft titleWith) + " : ")
    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine formattedValue


let private writeParagraph(title: string) (leftColumnWith: int) (formattedValue: string option) =
    Console.ForegroundColor <- ConsoleColor.Gray
    match formattedValue with
    | Some value -> 
        Console.WriteLine ((title.PadLeft leftColumnWith) + " : ")
        Console.ForegroundColor <- ConsoleColor.White
        Console.WriteLine value
    | None -> Console.WriteLine ((title.PadLeft leftColumnWith) + " : <none>")


let private formatTimeSpan(span: TimeSpan) : string =
    sprintf "%d days, %d hrs, %d minutes, %d seconds" span.Days span.Hours span.Minutes span.Seconds


let toConsole(status: Status) =
    Console.Clear()
    Console.BackgroundColor <- ConsoleColor.Black
    Console.SetCursorPosition(0, 0)
    let titleWidth = 19
    writeValue "Elapsed time"       titleWidth (status.ElapsedTime |> formatTimeSpan)
    writeValue "Stage"              titleWidth (status.StageName)
    writeValue "Executions"         titleWidth (status.Executions.ToString(CultureInfo.CurrentUICulture))
    writeValue "Crashes"            titleWidth (status.Crashes.ToString(CultureInfo.CurrentUICulture))
    writeValue "Nonzero exit codes" titleWidth (status.NonZeroExitCodes.ToString(CultureInfo.CurrentUICulture))
    writeValue "Paths"              titleWidth (status.Paths.ToString(CultureInfo.CurrentUICulture))
    writeValue "Executions/second"  titleWidth (status.ExecutionsPerSecond.ToString("G4", CultureInfo.CurrentUICulture))
    writeParagraph "Last crash"     titleWidth status.LastCrash
    status


let update(stageName: string, previousStatus: Status, currentResult: Result) : Status =
    previousStatus.AddExecution(stageName, currentResult) 
        |> toConsole

