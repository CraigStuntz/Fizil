namespace Fizil.Instrumentation

open System

// This type should be kept C# friendly!
[<AttributeUsage(AttributeTargets.Method)>]
type FizilEntryPointAttribute() =
    inherit System.Attribute()