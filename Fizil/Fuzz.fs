module Fuzz

open System.Collections
open TestCase

[<NoComparison>]
type Stage = {
    Name:      string
    TestCases: seq<byte[]>
}
/// Takes a data example uses it as a seed to generate 0 or more new test cases
type FuzzStrategy = byte[] -> Stage


let useOriginalExample : FuzzStrategy = 
    fun bytes -> 
        {
            Name =      "calibration"
            TestCases = Seq.singleton bytes 
        }


let private bitMasks(bitIndex: int, bitsToFlip: int) =
    match bitIndex % 8, bitsToFlip with 
    | 0, 1 -> 0b00000001uy, 0b00000000uy
    | 1, 1 -> 0b00000010uy, 0b00000000uy
    | 2, 1 -> 0b00000100uy, 0b00000000uy
    | 3, 1 -> 0b00001000uy, 0b00000000uy
    | 4, 1 -> 0b00010000uy, 0b00000000uy
    | 5, 1 -> 0b00100000uy, 0b00000000uy
    | 6, 1 -> 0b01000000uy, 0b00000000uy
    | 7, 1 -> 0b10000000uy, 0b00000000uy
    | 0, 2 -> 0b00000011uy, 0b00000000uy
    | 1, 2 -> 0b00000110uy, 0b00000000uy
    | 2, 2 -> 0b00001100uy, 0b00000000uy
    | 3, 2 -> 0b00011000uy, 0b00000000uy
    | 4, 2 -> 0b00110000uy, 0b00000000uy
    | 5, 2 -> 0b01100000uy, 0b00000000uy
    | 6, 2 -> 0b11000000uy, 0b00000000uy
    | 7, 2 -> 0b10000000uy, 0b00000001uy
    | 0, 4 -> 0b00001111uy, 0b00000000uy
    | 1, 4 -> 0b00011110uy, 0b00000000uy
    | 2, 4 -> 0b00111100uy, 0b00000000uy
    | 3, 4 -> 0b01111000uy, 0b00000000uy
    | 4, 4 -> 0b11110000uy, 0b00000000uy
    | 5, 4 -> 0b11100000uy, 0b00000001uy
    | 6, 4 -> 0b11000000uy, 0b00000011uy
    | 7, 4 -> 0b10000000uy, 0b00000111uy
    | bit, _ -> failwithf "Unsupported bit %d or bitsToFlip %d" bit bitsToFlip


/// Rolling bit flip with 1 bit stepover
let bitFlip (flipBits: int) : FuzzStrategy = 
    fun (bytes: byte[]) ->
        let totalBits = bytes.Length * 8
        let testCases = seq {
            for bit = 0 to totalBits - flipBits do
                let newBytes = Array.copy bytes
                let firstByte = bit / 8
                let firstByteMask, secondByteMask = bitMasks(bit, flipBits)
                let newFirstByte = bytes.[firstByte] ^^^ firstByteMask
                newBytes.[firstByte] <- newFirstByte
                let secondByte = firstByte + 1
                if secondByteMask <> 0uy && secondByte < bytes.Length
                then
                    let newSecondByte = bytes.[secondByte] ^^^ secondByteMask
                    newBytes.[secondByte] <- newSecondByte
                yield newBytes
        }
        {
            Name = sprintf "bitflip %d/1" flipBits
            TestCases = testCases
        }


/// Rolling byte flip with 1 byte stepover
let byteFlip (flipBytes: int) : FuzzStrategy = 
    fun (bytes: byte[]) ->
        let testCases = seq {
            for firstFlip = 0 to bytes.Length - 1 do
                let newBytes = Array.copy bytes
                let lastFlip = System.Math.Min(firstFlip + flipBytes, bytes.Length) - 1
                for flipByte = firstFlip to lastFlip do 
                    let newByte = bytes.[flipByte] ^^^ 0b11111111uy
                    newBytes.[flipByte] <- newByte
                yield newBytes
        }
        {
            Name = sprintf "byteflip %d/1" flipBytes
            TestCases = testCases
        }

/// Helper function to see if a particular change could
/// be a product of deterministic bit flips with the lengths and stepovers
/// attempted by the fuzzer. This is used to avoid dupes in some of the
/// deterministic fuzzing operations that follow bit flips. We also
/// return true if xoredOldAndNew is zero, which implies that the old and 
/// attempted new values are identical and the exec would be a waste of time.
let couldBeBitflip(oldValue: uint32, newValue: uint32) : bool =
    // implementation is more or less a direct translation of similar
    // function in afl-fuzz
    let xoredOldAndNew = oldValue ^^^ newValue
    let rec loop (value: uint32, shiftAmount: uint32) =
        match value with 
        | 0u -> false
        | _ when (value &&& 1u = 0u) ->
            let shifted = value >>> 1
            let newShiftAmount = shiftAmount + 1u
            loop(shifted, newShiftAmount)
        | 1u | 3u | 15u -> true
        | _ when (shiftAmount &&& 7u = 7u) ->
            // 8-, 16-, and 32-bit patterns are OK only if shift factor is
            // divisible by 8, since that's the stepover for these ops.
            false
        | 0xffu | 0xffffu | 0xffffffffu -> true
        | _ -> false
    loop (xoredOldAndNew, 0u)


let arithMax = 16uy;

let arith8 : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for i = 0 to bytes.Length - 1 do
                let oldByte = bytes.[i]
                let newBytes = 
                    Array.concat [|
                        ([| 1uy .. arithMax |] |> Array.rev |> Array.map (fun offset -> oldByte - offset))
                        ([| 1uy .. arithMax |] |> Array.map (fun offset -> oldByte + offset)) |]
                for newByte in newBytes do
                    if not (couldBeBitflip(uint32(oldByte), uint32(newByte)))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i] <- newByte
                        yield newBytes
        }
        {
            Name = "arith 8/8"
            TestCases = testCases
        }


let swap16 (value: uint16) =
    (value <<< 8) ||| (value >>> 8)


let swap32 (value: uint32) =
    (value <<< 24) 
    ||| ((value <<< 8) &&& 0x00FF0000u)
    ||| ((value >>> 8) &&& 0x0000FF00u)
    ||| (value >>> 24)


let toBytes (value: uint32) =
    (
        uint8(value >>> 24),
        uint8(value >>> 16),
        uint8(value >>> 8),
        uint8(value)
    )


let arith16 : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for i = 0 to bytes.Length - 2 do
                for addend = 1us to uint16(arithMax) do
                    let origLE = uint16(bytes.[i]) + (uint16(bytes.[i + 1]) * 256us)
                    let origBE = swap16 origLE
                    let newWordLEPlus  = origLE + addend
                    let newWordLEMinus = origLE - addend
                    let newWordBEPlus  = swap16 (origBE + addend)
                    let newWordBEMinus = swap16 (origBE - addend)
                    // Try little endian addition and subtraction first. Do it only
                    // if the operation would affect more than one byte (hence the 
                    // & 256us overflow checks) and if it couldn't be a product of
                    // a bitflip. */
                    if ((origLE &&& 0xffus) + addend > 0xffus) && not(couldBeBitflip(uint32(origLE), uint32(newWordLEPlus)))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i]     <- uint8(newWordLEPlus % 256us)
                        newBytes.[i + 1] <- uint8(newWordLEPlus / 256us)
                        yield newBytes
                    if ((origLE &&& 0xffus) < addend) && not(couldBeBitflip(uint32(origLE), uint32(newWordLEMinus)))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i]     <- uint8(newWordLEMinus % 256us)
                        newBytes.[i + 1] <- uint8(newWordLEMinus / 256us)
                        yield newBytes
                    if ((origBE &&& 0xffus) + addend > 0xffus) && not(couldBeBitflip(uint32(origBE), uint32(newWordBEPlus)))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i]     <- uint8(newWordBEPlus % 256us)
                        newBytes.[i + 1] <- uint8(newWordBEPlus / 256us)
                        yield newBytes
                    if ((origLE &&& 0xffus) < addend) && not(couldBeBitflip(uint32(origBE), uint32(newWordBEMinus)))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i]     <- uint8(newWordBEMinus % 256us)
                        newBytes.[i + 1] <- uint8(newWordBEMinus / 256us)
                        yield newBytes
        }
        {
            Name = "arith 16/8"
            TestCases = testCases
        }


let arith32 : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for i = 0 to bytes.Length - 4 do
                for addend = 1u to uint32(arithMax) do
                    let origLE = 
                        uint32(bytes.[i]) 
                        + (uint32(bytes.[i + 1]) * 0x0000FFu)
                        + (uint32(bytes.[i + 2]) * 0x00FF00u)
                        + (uint32(bytes.[i + 3]) * 0xFF0000u)

                    let origBE = swap32 origLE
                    let newWordLEPlus  = origLE + addend
                    let newWordLEMinus = origLE - addend
                    let newWordBEPlus  = swap32 (origBE + addend)
                    let newWordBEMinus = swap32 (origBE - addend)
                    // Try little endian addition and subtraction first. Do it only
                    // if the operation would affect more than one byte (hence the 
                    // & 256us overflow checks) and if it couldn't be a product of
                    // a bitflip. */
                    if ((origLE &&& 0xffffu) + addend > 0xffffu) && not(couldBeBitflip(uint32(origLE), uint32(newWordLEPlus)))
                    then 
                        let newBytes = Array.copy bytes
                        let b3, b2, b1, b0 = newWordLEPlus |> toBytes
                        newBytes.[i]     <- b0
                        newBytes.[i + 1] <- b1
                        newBytes.[i + 2] <- b2
                        newBytes.[i + 3] <- b3
                        yield newBytes
                    if ((origLE &&& 0xffffu) < addend) && not(couldBeBitflip(uint32(origLE), uint32(newWordLEMinus)))
                    then 
                        let newBytes = Array.copy bytes
                        let b3, b2, b1, b0 = newWordLEMinus |> toBytes
                        newBytes.[i]     <- b0
                        newBytes.[i + 1] <- b1
                        newBytes.[i + 2] <- b2
                        newBytes.[i + 3] <- b3
                        yield newBytes
                    if ((origBE &&& 0xffffu) + addend > 0xffffu) && not(couldBeBitflip(uint32(origBE), uint32(newWordBEPlus)))
                    then 
                        let newBytes = Array.copy bytes
                        let b3, b2, b1, b0 = newWordBEPlus |> toBytes
                        newBytes.[i]     <- b0
                        newBytes.[i + 1] <- b1
                        newBytes.[i + 2] <- b2
                        newBytes.[i + 3] <- b3
                        yield newBytes
                    if ((origLE &&& 0xffffu) < addend) && not(couldBeBitflip(uint32(origBE), uint32(newWordBEMinus)))
                    then 
                        let newBytes = Array.copy bytes
                        let b3, b2, b1, b0 = newWordBEMinus |> toBytes
                        newBytes.[i]     <- b0
                        newBytes.[i + 1] <- b1
                        newBytes.[i + 2] <- b2
                        newBytes.[i + 3] <- b3
                        yield newBytes
        }
        {
            Name = "arith 32/8"
            TestCases = testCases
        }


/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let private allStrategies = [ 
    useOriginalExample
    bitFlip 1
    bitFlip 2
    bitFlip 4
    byteFlip 1
    byteFlip 2
    byteFlip 4
    arith8
    arith16
    arith32
]

let private applyStrategy (strategy: FuzzStrategy) (examples: TestCase list) : seq<TestCase> = 
    seq {
        for example in examples do
            let stage = strategy(example.Data)
            for input in stage.TestCases do
                yield { example with Data = input; SourceFile = None; Stage = stage.Name }
    }


let all (examples: TestCase list) : seq<TestCase> =     
    seq {
        for strategy in allStrategies do
            yield applyStrategy strategy examples
    } |> Seq.collect id
