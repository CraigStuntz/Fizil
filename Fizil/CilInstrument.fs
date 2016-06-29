module CilInstrument

open Mono.Cecil
open Mono.Cecil.Cil
open Mono.Cecil.Rocks
open Mono.Collections.Generic
open Fizil.Instrumentation
open System
open System.Linq


[<NoComparison>]
type private InstrumentState = {
    Trace:      MethodReference
    Random:     Random
}


let private traceMethod   = typeof<Instrument>.GetMethod(Fizil.Instrumentation.Instrument.TraceMethodName)


let private insertTraceInstruction(ilProcessor: ILProcessor, before: Instruction, state: InstrumentState) =
    let compileTimeRandom = state.Random.Next(0, UInt16.MaxValue |> Convert.ToInt32)
    let ldArg             = ilProcessor.Create(OpCodes.Ldc_I4, compileTimeRandom)
    let callTrace         = ilProcessor.Create(OpCodes.Call, state.Trace)
    ilProcessor.InsertBefore(before, ldArg)
    ilProcessor.InsertAfter (ldArg, callTrace)


let private instrumentMethod (state: InstrumentState) (definition: MethodDefinition) = 
    if definition <> definition.Module.EntryPoint // entry point is already instrumented
    then
        let ilProcessor       = definition.Body.GetILProcessor()
        let firstInstruction  = definition.Body.Instructions.[0]
        insertTraceInstruction(ilProcessor, firstInstruction, state)


let private instrumentType (state: InstrumentState) (definition: TypeDefinition) = 
    definition.Methods 
        |> Seq.filter (fun def -> def.Body <> null)
        |> Seq.iter (instrumentMethod state)


//   Entry point instrumentation...
//
//   IL_0000:  nop
//   IL_0001:  newobj     instance void [Fizil.Instrumentation]Fizil.Instrumentation.Instrument::.ctor()
//   IL_0007:  stsfld     class [Fizil.Instrumentation]Fizil.Instrumentation.Instrument target.Program::<>f__instrument
//   .try
//   {
//      ...existing entry point code
//   }  // end .try
//   finally
//   {
//     IL_007c:  ldsfld.0
//     IL_007d:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
//     IL_0083:  endfinally
//   }  // end handler
//   IL_0084:  ret
// }  // end of method Program::Main

[<NoComparison>]
type MethodReturnType = 
    | ReturnsVoid
    | ReturnsNonVoid of VariableDefinition

let private rewriteRetAndFixup(ret: Instruction, target: Instruction, ilProcessor: ILProcessor, originalInstructions: Collection<Instruction>, returnType: MethodReturnType) =
    let leave = Instruction.Create(OpCodes.Leave, target)
    ilProcessor.Replace(ret, leave)
    match returnType with
    | ReturnsNonVoid returnVariable -> ilProcessor.InsertBefore(leave, Instruction.Create(OpCodes.Stloc, returnVariable))
    | ReturnsVoid -> ()
    
    // Any instruction which jumped to the old ret should now jump to the new leave
    originalInstructions |> Seq.iteri (fun index instruction -> 
        if ret.Equals(instruction.Operand)
        then ilProcessor.Replace(instruction, Instruction.Create(instruction.OpCode, leave))
    )
    

let private convertRetsToLeaves(methodDefinition: MethodDefinition, ilProcessor: ILProcessor) : Instruction =
    let instructions = methodDefinition.Body.Instructions
    let originalInstructions = Collection<Instruction>(instructions) 
    let lastRet = ilProcessor.Create(OpCodes.Ret)
    ilProcessor.Append lastRet
    if methodDefinition.ReturnType = methodDefinition.Module.TypeSystem.Void 
    then
        originalInstructions |> Seq.iteri (fun index instruction ->
                if instruction.OpCode = OpCodes.Ret && (instruction <> lastRet)
                then rewriteRetAndFixup(instruction, lastRet, ilProcessor, originalInstructions, ReturnsVoid)
            )
        lastRet
    else
        let returnVariable = VariableDefinition("<>v__return", methodDefinition.ReturnType)
        methodDefinition.Body.Variables.Add(returnVariable)
        let lastLd = Instruction.Create(OpCodes.Ldloc, returnVariable)
        ilProcessor.InsertBefore(lastRet, lastLd)
        // will be mutating original collection, so make a copy to iterate
        originalInstructions |> Seq.iteri (fun index instruction ->
                if (instruction.OpCode = OpCodes.Ret && instruction <> lastRet)
                then 
                    rewriteRetAndFixup(instruction, lastLd, ilProcessor, originalInstructions, ReturnsNonVoid returnVariable)
            )
        lastLd
    

[<Literal>]
let private instrumentFieldAttributes : FieldAttributes = 
    FieldAttributes.Static
    ||| FieldAttributes.Public


let private instrumentEntryPoint(assembly: AssemblyDefinition) =
    let instrumentTypeRef  = assembly.EntryPoint.Module.Import(typeof<Instrument>)
    let body               = assembly.EntryPoint.Body
    body.SimplifyMacros()
    let ilProcessor       = body.GetILProcessor()
    let closeMethodRef    = assembly.EntryPoint.Module.Import(typeof<Instrument>.GetMethod(Instrument.CloseMethodName))
    let callClose         = ilProcessor.Create(OpCodes.Call, closeMethodRef)
    let existingFirstInst = body.Instructions.First()
    let newExitInsts      = convertRetsToLeaves(assembly.EntryPoint, ilProcessor)
    // before try
    let openMethodRef    = assembly.EntryPoint.Module.Import(typeof<Instrument>.GetMethod(Instrument.OpenMethodName))
    ilProcessor.InsertBefore(existingFirstInst, ilProcessor.Create(OpCodes.Call, openMethodRef))
    // try block here
    //    ... existing code
    // finally block
    ilProcessor.InsertBefore(newExitInsts, callClose)
    ilProcessor.InsertAfter(callClose, ilProcessor.Create(OpCodes.Endfinally))
    let finallyHandler    = ExceptionHandler(ExceptionHandlerType.Finally)
    finallyHandler.TryStart     <- existingFirstInst
    finallyHandler.TryEnd       <- callClose
    finallyHandler.HandlerStart <- callClose
    finallyHandler.HandlerEnd   <- newExitInsts
    body.ExceptionHandlers.Add finallyHandler
    // end finally
    body.OptimizeMacros()


let instrumentExecutable (assemblyFilename: string, outputFileName: string) =
    let assembly     = AssemblyDefinition.ReadAssembly assemblyFilename
    if assembly.EntryPoint = null 
    then 
        failwith "TODO: Handle this case!"
    else 
        let instrument   = instrumentEntryPoint assembly
        let trace        = assembly.MainModule.Import traceMethod
        let initialState = { Random = Random(); Trace = trace }
        let mainModuleTypes = assembly.MainModule.Types
        assembly.MainModule.AssemblyReferences |> Seq.iter (fun reference ->
            reference.PublicKeyToken <- null
        )
        mainModuleTypes |> Seq.iter (instrumentType initialState)
        assembly.Write outputFileName
        instrument


let removeStrongName (assemblyDefinition : AssemblyDefinition) =
    let name = assemblyDefinition.Name;
    name.HasPublicKey <- false;
    name.PublicKey <- Array.empty;
    assemblyDefinition.Modules |> Seq.iter (fun moduleDefinition ->
        moduleDefinition.Attributes <- moduleDefinition.Attributes &&& ~~~ModuleAttributes.StrongNameSigned)
    let aptca = assemblyDefinition.CustomAttributes.FirstOrDefault(fun attr -> attr.AttributeType.FullName = typeof<System.Security.AllowPartiallyTrustedCallersAttribute>.FullName)
    assemblyDefinition.CustomAttributes.Remove aptca |> ignore


let instrumentDependency (assemblyFilename: string, outputFileName: string) =
    let assembly = AssemblyDefinition.ReadAssembly(assemblyFilename)
    removeStrongName(assembly)
    let trace        = assembly.MainModule.Import traceMethod
    let initialState = { Random = Random(); Trace = trace }
    let mainModuleTypes = assembly.MainModule.Types
    mainModuleTypes |> Seq.iter (instrumentType initialState)
    assembly.Write(outputFileName)


