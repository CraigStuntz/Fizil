module SharedMemory

open System.IO
open System.IO.MemoryMappedFiles


let private mapSize32 = (System.UInt16.MaxValue |> int32) + 1
let private mapSize64 = mapSize32 |> int64

let private mapName = "Fizil-shared-memory"

/// Creates and zero-initializes a memory-mapped file
let create() =
    // this will be zeroed by the OS.
    // "The initial contents of the pages in a file mapping object backed by the operating system paging file are 0 (zero)."
    // https://msdn.microsoft.com/en-us/library/aa366537(v=vs.85).aspx
    MemoryMappedFile.CreateNew(
        mapName,
        mapSize64,
        MemoryMappedFileAccess.ReadWrite,
        MemoryMappedFileOptions.None,
        HandleInheritability.Inheritable)

let openMemory() =
    MemoryMappedFile.OpenExisting(
        mapName,
        MemoryMappedFileRights.ReadWrite)


let incrementByte(sharedMemory : MemoryMappedFile) (address: uint16) =
    let offset = address |> int64
    let accessor = sharedMemory.CreateViewAccessor(offset, 1L)
    let b = accessor.ReadByte(0L)
    accessor.Write(0L, b + 1uy)


let readBytes(sharedMemory: MemoryMappedFile) : byte[] =
    let stream = sharedMemory.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read)
    let reader = new BinaryReader(stream)
    reader.ReadBytes(mapSize32)