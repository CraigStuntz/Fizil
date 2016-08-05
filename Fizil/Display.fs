module Display

open ExecutionResult
open System
open System.Globalization


let private consoleTitleRedrawInterval = TimeSpan(0, 0, 0, 15, 0)


type private Status = 
    {
        StartTime:           DateTimeOffset
        ElapsedTime:         TimeSpan
        StageName:           string
        Executions:          uint64
        Crashes:             uint64
        NonZeroExitCodes:    uint64
        Paths:               uint64
        ExecutionsPerSecond: float
        LastCrash:           string option
        LastTitleRedraw:     DateTimeOffset
        ShouldRedrawTitles:  bool
    }
    with 
        member this.AddExecution(result: Result) =
            let now         = DateTimeOffset.UtcNow 
            let elapsedTime = now - this.StartTime
            let executions  = this.Executions + 1UL
            let executionsPerSecond = 
                if (elapsedTime.TotalMilliseconds > 0.0) 
                then Convert.ToDouble(executions) / Convert.ToDouble(elapsedTime.TotalMilliseconds) * 1000.0
                else 0.0
            let shouldRedrawTitles = this.Executions = 0UL || now - this.LastTitleRedraw > consoleTitleRedrawInterval
            let lastTitleRedraw = if shouldRedrawTitles then now else this.LastTitleRedraw
            {
                StartTime           = this.StartTime
                ElapsedTime         = elapsedTime
                StageName           = result.TestCase.Stage
                Executions          = executions
                Crashes             = this.Crashes          + (if result.Crashed       then 1UL else 0UL)
                NonZeroExitCodes    = this.NonZeroExitCodes + (if result.ExitCode <> 0 then 1UL else 0UL)
                Paths               = this.Paths            + (if result.NewPathFound  then 1UL else 0UL)
                ExecutionsPerSecond = executionsPerSecond
                LastCrash           = 
                    match result.Crashed, result.HasStdErrOutput with
                    | true, true  -> Some result.StdErr
                    | true, false -> Some result.StdOut
                    | false, _    -> this.LastCrash
                LastTitleRedraw     = lastTitleRedraw
                ShouldRedrawTitles  = shouldRedrawTitles 
            }


let private initialState() = 
    {
        StartTime           = DateTimeOffset.UtcNow
        ElapsedTime         = TimeSpan.Zero
        StageName           = "initializing"
        Executions          = 0UL
        Crashes             = 0UL
        NonZeroExitCodes    = 0UL
        Paths               = 0UL
        ExecutionsPerSecond = 0.0
        LastCrash           = None
        LastTitleRedraw     = DateTimeOffset.UtcNow
        ShouldRedrawTitles  = true
    }


let private writeValue (redrawTitle: bool) (title: string) (titleWidth: int) (formattedValue: string) =
    if redrawTitle
    then 
        Console.ForegroundColor <- ConsoleColor.Gray
        Console.Write ((title.PadLeft titleWidth) + " : ")
    else
        Console.CursorLeft <- titleWidth + 3
    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine (formattedValue + "   ")


let private writeParagraph (redrawTitle: bool) (title: string) (leftColumnWidth: int) (formattedValue: string option) =
    Console.ForegroundColor <- ConsoleColor.Gray
    match formattedValue with
    | Some value -> 
        if redrawTitle
        then
            Console.WriteLine ((title.PadLeft leftColumnWidth) + " : ")
        else
            Console.CursorTop <- Console.CursorTop + 1
        Console.ForegroundColor <- ConsoleColor.White
        Console.WriteLine value
    | None -> 
        if redrawTitle
        then 
            Console.Write ((title.PadLeft leftColumnWidth) + " : ")
        else
            Console.CursorLeft <- leftColumnWidth + 3
        Console.WriteLine "<none>"


let private formatTimeSpan(span: TimeSpan) : string =
    sprintf "%d days, %d hrs, %d minutes, %d seconds  " span.Days span.Hours span.Minutes span.Seconds


let private toConsole(status: Status) =
    Console.BackgroundColor <- ConsoleColor.Black
    Console.SetCursorPosition(0, 0)
    let titleWidth = 19
    let redrawTitle = status.ShouldRedrawTitles
    writeValue redrawTitle "Elapsed time"       titleWidth (status.ElapsedTime |> formatTimeSpan)
    writeValue redrawTitle "Stage"              titleWidth (status.StageName)
    writeValue redrawTitle "Executions"         titleWidth (status.Executions.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Crashes"            titleWidth (status.Crashes.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Nonzero exit codes" titleWidth (status.NonZeroExitCodes.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Paths"              titleWidth (status.Paths.ToString(CultureInfo.CurrentUICulture))
    writeValue redrawTitle "Executions/second"  titleWidth (status.ExecutionsPerSecond.ToString("G4", CultureInfo.CurrentUICulture))
    writeParagraph redrawTitle "Last crash"     titleWidth status.LastCrash


let private agent: MailboxProcessor<Result> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop (state: Status) = async {
             let! result = inbox.Receive()
             let state' = state.AddExecution result
             state' |> toConsole
             return! loop state'
        }
        loop (initialState()))


let postResult(result: Result) =
    agent.Post result

