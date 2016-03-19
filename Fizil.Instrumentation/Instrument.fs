namespace Fizil.Instrumentation

open System.IO.MemoryMappedFiles
open SharedMemory


type Instrument() = 
    let mutable previousLocation : uint16           = 0us

    let mutable sharedMemory     : MemoryMappedFile = SharedMemory.openMemory()

    static member TraceMethodName = "Trace" 
    member this.Trace (currentLocation: uint16) =
        let address = currentLocation ^^^ previousLocation
        SharedMemory.incrementByte sharedMemory address
        previousLocation <- (currentLocation >>> 1)


    interface System.IDisposable with
        member this.Dispose() = 
            sharedMemory.Dispose()