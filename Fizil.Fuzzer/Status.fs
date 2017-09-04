module Status

open ExecutionResult
open System


type Configuration = {
    ExampleBytes: int
    ExampleCount: int
    StartTime:    DateTimeOffset
}


[<NoComparison>]
type Message = 
    | Initialize of Configuration
    | Update     of Result


type StatusMonitor = {
    postResult: Message -> unit
}
