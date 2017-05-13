namespace Fizil.Tracing

//open Microsoft.Diagnostics.Tracing
//open Microsoft.Diagnostics.Tracing.Parsers
//open Microsoft.Diagnostics.Tracing.Session
//
//[<Sealed>]
//type Trace()  = 
//    inherit System.Diagnostics.Tracing.EventSource(true)
//
//    let TraceEventId = 1
//
//    member this.Log(currentLocation: uint16) = 
//        this.WriteEvent(TraceEventId, currentLocation)
//
//    static member Event = new Trace()
//
//    static member Collect() = 
//        let providerGuid = System.Guid.NewGuid()
//        use session = new TraceEventSession("MySession", null)
//        use source = new Microsoft.Diagnostics.Tracing.ETWTraceEventSource("MySession", TraceEventSourceType.Session)
//        let parser = DynamicTraceEventParser(source)
//
//        session.EnableProvider(providerGuid) |> ignore
//        source.Process()
