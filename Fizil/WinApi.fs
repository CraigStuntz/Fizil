module WinApi

open System
open System.IO
open System.Runtime.InteropServices
open System.Text


// Error modes

[<Flags>]
type ErrorModes =
    | SYSTEM_DEFAULT             = 0us
    | SEM_FAILCRITICALERRORS     = 1us
    | SEM_NOALIGNMENTFAULTEXCEPT = 4us
    | SEM_NOGPFAULTERRORBOX      = 2us
    | SEM_NOOPENFILEERRORBOX     = 32768us


[<DllImport("kernel32.dll")>]
extern ErrorModes SetErrorMode(ErrorModes uMode);


let disableCrashReporting() =
    SetErrorMode(ErrorModes.SEM_NOGPFAULTERRORBOX) |> ignore


// Exit codes
let ClrUnhandledExceptionCode = -532462766


// Paths

let private FILE_ATTRIBUTE_DIRECTORY : int = 0x10
let private FILE_ATTRIBUTE_NORMAL : int = 0x80
let private MAX_PATH : int = 260

[<DllImport("shlwapi.dll", CharSet = CharSet.Auto)>]
extern bool PathRelativePathTo(
    StringBuilder pszPath, 
    string        pszFrom, 
    int           dwAttrFrom, 
    string        pszTo, 
    int           dwAttrTo);


let getRelativePath (fromPath: string) (toPath: string) : string =
    let path = StringBuilder(MAX_PATH)
    match PathRelativePathTo(path, fromPath, FILE_ATTRIBUTE_DIRECTORY, toPath, FILE_ATTRIBUTE_DIRECTORY) with
    | false -> toPath
    | true  -> path.ToString()


