module FuzzStrategies

open System.Collections

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

/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let all = [ 
    useOriginalExample
    arith8
    bitFlip 1
    bitFlip 2
    bitFlip 4
    byteFlip 1
    byteFlip 2
    byteFlip 4
    
]
                

