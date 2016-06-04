namespace Fizil.Instrumentation

open System.IO.MemoryMappedFiles
open SharedMemory

[<AllowNullLiteral>]
[<Sealed>]
type Instrument()  = 
    let mutable previousLocation : uint16           = 0us

    let mutable sharedMemory     : MemoryMappedFile = SharedMemory.openMemory()

    static let mutable instance : Instrument = null

    static member OpenMethodName = "Open" 
    static member Open() = instance <- new Instrument()

    static member TraceMethodName = "Trace" 
    static member Trace (currentLocation: uint16) = instance.TraceImpl(currentLocation)

    member this.TraceImpl (currentLocation: uint16) =
        let address = currentLocation ^^^ previousLocation
        SharedMemory.incrementByte sharedMemory address
        previousLocation <- (currentLocation >>> 1)

    interface System.IDisposable with
        member this.Dispose() = 
            sharedMemory.Dispose()

    static member CloseMethodName = "Close" 
    static member Close() = (instance :> System.IDisposable).Dispose();