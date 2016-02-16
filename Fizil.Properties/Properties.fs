namespace Fizil.Properties

open System.Collections.Generic

// All code here must be kept "C# friendly!"

type TestRun = {
    Input: byte[]
    ExitCode: int
    StdOut: string 
    StdErr: string
} 
with 
    member this.InputAsString() =
        let chars: char[] = Array.create (this.Input.Length / sizeof<char>) System.Char.MinValue
        System.Buffer.BlockCopy(this.Input, 0, chars, 0, this.Input.Length)
        System.String(chars)




type PropertyCheckResult = {
    Verified: bool
    Message: string
} 


type ISuccessfulExecutionProperty =
    abstract member CheckSuccessfulExecution: TestRun -> PropertyCheckResult


type IPropertyChecker = 
    abstract member SuccessfulExecutionProperties: seq<ISuccessfulExecutionProperty>