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
            for bit = 0 to totalBits - 1 do
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


/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let all = [ 
    useOriginalExample
    bitFlip 1
    bitFlip 2
    bitFlip 4
]
                

