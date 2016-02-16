namespace Fizil.Test

open Fizil.Properties
open System.Collections.Generic


type TestProperty() =
    interface ISuccessfulExecutionProperty with
        member this.CheckSuccessfulExecution (testRun : TestRun) =
            {
                Verified = not <| testRun.StdOut.Contains "d"
                Message  = "Property not verified"
            }

type TestProperties() =
    interface IPropertyChecker with 
        member this.SuccessfulExecutionProperties =
            [ TestProperty() :> ISuccessfulExecutionProperty ] |> Seq.ofList

