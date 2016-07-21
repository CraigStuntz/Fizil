module FuzzStrategies

open System.Collections

/// Takes a data example uses it as a seed to generate 0 or more new test cases
type FuzzStrategy = byte[] -> seq<byte[]>


let useOriginalExample : FuzzStrategy = 
    Seq.singleton


let private bitMasks(bitIndex: int, bitsToFlip: int) =
    match bitIndex % 8, bitsToFlip with 
    | 0, 1 -> 1uy,   0uy
    | 1, 1 -> 2uy,   0uy
    | 2, 1 -> 4uy,   0uy
    | 3, 1 -> 8uy,   0uy
    | 4, 1 -> 16uy,  0uy
    | 5, 1 -> 32uy,  0uy
    | 6, 1 -> 64uy,  0uy
    | 7, 1 -> 128uy, 0uy
    | 0, 2 -> 3uy,   0uy
    | 1, 2 -> 6uy,   0uy
    | 2, 2 -> 12uy,  0uy
    | 3, 2 -> 24uy,  0uy
    | 4, 2 -> 48uy,  0uy
    | 5, 2 -> 96uy,  0uy
    | 6, 2 -> 192uy, 0uy
    | 7, 2 -> 128uy, 1uy
    | 0, 4 -> 15uy,  0uy
    | 1, 4 -> 30uy,  0uy
    | 2, 4 -> 60uy,  0uy
    | 3, 4 -> 120uy, 0uy
    | 4, 4 -> 240uy, 0uy
    | 5, 4 -> 224uy, 1uy
    | 6, 4 -> 192uy, 3uy
    | 7, 4 -> 128uy, 5uy
    | bit, _ -> failwithf "Unsupported bit %d or bitsToFlip %d" bit bitsToFlip


/// Rolling bit flip with 1 bit stepover
let bitFlip (flipBits: int) : FuzzStrategy = 
    fun (bytes: byte[]) ->
        let totalBits = bytes.Length * 8
        seq {
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


/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let all = [ 
    useOriginalExample
    bitFlip 1
    bitFlip 2
    bitFlip 4
]
                

