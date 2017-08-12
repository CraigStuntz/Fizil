module Fuzz

open System.Collections
open TestCase

/// Takes a data example uses it as a seed to generate 0 or more new test cases
type FuzzStrategy = byte[] -> Stage


let useOriginalExample : FuzzStrategy = 
    fun bytes -> 
        {
            Name =                "calibration"
            TestCasesPerExample = TestCasesPerExample 1
            TestCases =           Seq.singleton bytes 
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
            Name =                sprintf "bitflip %d/1" flipBits
            TestCasesPerExample = TestCasesPerByte 8
            TestCases =           testCases
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
            Name =                sprintf "byteflip %d/1" flipBytes
            TestCasesPerExample = TestCasesPerByte 1
            TestCases =           testCases
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
            TestCasesPerExample = TestCasesPerByte ((int arithMax) * 2)
            TestCases = testCases
        }


let swap16 (value: uint16) =
    (value <<< 8) ||| (value >>> 8)

let swap16s (value: int16) =
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
            TestCasesPerExample = TestCasesPerByte ((int arithMax) * 2)
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
            TestCasesPerExample = TestCasesPerByte ((int arithMax) * 2)
            TestCases = testCases
        }


let inline difference a b = if a > b then a - b else b - a


let couldBeArith(oldVal: uint32, newVal: uint32, numberOfBytes: uint8) : bool =
    // implementation is more or less a direct translation of similar
    // function in afl-fuzz
    if (oldVal = newVal) 
    then true 
    else
        // See if one-byte adjustments to any byte could produce this result.
        let rec loop1 (diffs: uint8, ov: uint8, nv: uint8, byteIndex: int) =
            if ((uint8 byteIndex) = numberOfBytes)
            then (diffs, ov, nv)
            else 
                let a = oldVal >>> (8 * byteIndex) |> uint8
                let b = newVal >>> (8 * byteIndex) |> uint8

                if (a <> b)
                then loop1 (diffs + 1uy, a, b, byteIndex + 1)
                else loop1 (diffs, ov, nv, byteIndex + 1)
        let (diffs1, ov1, nv1) = loop1(0uy, 0uy, 0uy, 0)

        // If only one byte differs and the values are within range, return 1. */

        if (diffs1 = 1uy) 
            && ((difference ov1 nv1) <= arithMax)
        then true
        else
            if (numberOfBytes = 1uy)
            then false
            else
                // See if two-byte adjustments to any byte would produce this result
                let rec loop2 (diffs: uint8, ov: uint16, nv: uint16, byteIndex: int) =
                    if (uint8 byteIndex) = (numberOfBytes / 2uy)
                    then (diffs, ov, nv)
                    else 
                        let a = oldVal >>> (16 * byteIndex) |> uint16
                        let b = newVal >>> (16 * byteIndex) |> uint16

                        if (a <> b)
                        then loop2 (diffs + 1uy, a, b, byteIndex + 1)
                        else loop2 (diffs, ov, nv, byteIndex + 1)
                let (diffs2, ov2, nv2) = loop2(0uy, 0us, 0us, 0)

                // If only one word differs and the values are within range, return 1.

                if diffs2 = 1uy
                then 
                    if (difference ov2 nv2) <= (uint16 arithMax)
                    then true
                    else 
                        if (difference (swap16 ov2) (swap16 nv2)) <= (uint16 arithMax)
                        then true
                        else
                            // Finally, let's do the same thing for dwords.
                            numberOfBytes = 4uy 
                                && ((difference oldVal newVal) <= (uint32 arithMax)
                                    || (difference (swap32 oldVal) (swap32 newVal) <= (uint32 arithMax)))
                else false


let private dictionary(values: byte[][]) : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for value in values do 
            for i = 0 to bytes.Length - value.Length do
                let newBytes = Array.copy bytes
                for j = 0 to value.Length - 1 do
                    newBytes.[i + j] <- value.[j]
                yield newBytes
        }
        {
            Name = "dictionary"
            TestCasesPerExample = TestCasesPerByte (values |> Array.length)
            TestCases = testCases
        }

let private interesting8 = [|
   -128y         // Overflow signed 8-bit when decremented  
   -1y           //                                         
   0y            //                                         
   1y            //                                         
   16y           // One-off with common buffer size         
   32y           // One-off with common buffer size         
   64y           // One-off with common buffer size         
   100y          // One-off with common buffer size         
   127y          // Overflow signed 8-bit when incremented  
|]

let private _interest16 = [|
   -32768s       // Overflow signed 16-bit when decremented 
   -129s         // Overflow signed 8-bit                   
   128s          // Overflow signed 8-bit                   
   255s          // Overflow unsig 8-bit when incremented   y
   256s          // Overflow unsig 8-bit                    
   512s          // One-off with common buffer size         
   1000s         // One-off with common buffer size         
   1024s         // One-off with common buffer size         
   4096s         // One-off with common buffer size         
   32767s        // Overflow signed 16-bit when incremented */
|]

let private interesting16 = 
    Array.concat [
        interesting8 |> Array.map int16
        _interest16
    ]

let private _interest32 = [|
   -2147483648   // Overflow signed 32-bit when decremented 
   -100663046    // Large negative number (endian-agnostic) 
   -32769        // Overflow signed 16-bit                  
   32768         // Overflow signed 16-bit                  
   65535         // Overflow unsig 16-bit when incremented  
   65536         // Overflow unsig 16 bit                   
   100663045     // Large positive number (endian-agnostic) 
   2147483647    // Overflow signed 32-bit when incremented */
|]

let private interesting32 = 
    Array.concat [
        interesting8 |> Array.map int32
        _interest16  |> Array.map int32
        _interest32
    ]


let private crossproduct l1 l2 =
  seq { for el1 in l1 do
          for el2 in l2 do
            yield el1, el2 }


/// See if insertion of an 
/// interesting integer is redundant given the insertions done for
/// shorter numBytes. The last param (checkLE) is set if the caller
/// already executed LE insertion for current numBytes and wants to see
/// if BE variant passed in new_val is unique.
let couldBeInterest(oldVal : uint32, newVal : uint32, numBytes : int, checkLE : bool) : bool =
    // implementation is more or less a direct translation of similar
    // function in afl-fuzz
    if (oldVal = newVal) 
    then true
    else
        // See if one-byte insertions from interesting_8 over oldVal could
        // produce newVal.  
        let shifts = [| 0 .. (numBytes - 1) |]
        let shiftAndInterest8 = crossproduct shifts interesting8
        let matches (shift: int, interest: sbyte) =
            let tval = (oldVal &&& ~~~(0xffu <<< (shift * 8))) 
                       ||| ((uint32 interest) <<< (shift * 8))
            newVal = tval
        if shiftAndInterest8 |> Seq.exists matches
        then true 
        else  
            // Bail out unless we're also asked to examine two-byte LE insertions
            // as a preparation for BE attempts. 
            if (numBytes = 2) && (not checkLE)
            then false
            else
                // See if two-byte insertions over oldVal could give us newVal.
                let shifts = [| 0 .. (numBytes - 2) |]
                let shiftAndInterest16 = crossproduct shifts interesting16
                let matches (shift: int, interest: int16) =
                    let tval = (oldVal &&& ~~~(0xffffu <<< (shift * 8)))
                            ||| ((uint32 interest) <<< (shift * 8))
                    if newVal = tval
                    then true 
                    else
                        // Continue here only if blen > 2. 
                        let tval = (oldVal &&& ~~~(0xffffu <<< (shift * 8)))
                                ||| ((uint32 (swap16s interest)) <<< (shift * 8))
                        newVal = tval
                if shiftAndInterest16 |> Seq.exists matches
                then true
                else 
                    if (numBytes = 4 && checkLE) 
                    then 
                        // See if four-byte insertions could produce the same result (LE only)
                        Seq.exists (fun interest -> (uint32 interest) = newVal) interesting32
                    else false


let interest8 : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for i = 0 to bytes.Length - 1 do
                for interesting in interesting8 do 
                    let oldByte = bytes.[i]
                    if not (couldBeBitflip(uint32(oldByte), uint32(interesting))
                        || couldBeArith(uint32(oldByte), uint32(interesting), 1uy))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i] <- (uint8 interesting)
                        yield newBytes
        }
        {
            Name = "interesting8"
            TestCasesPerExample = TestCasesPerByte (interesting8 |> Array.length)
            TestCases = testCases
        }


let interest16 : FuzzStrategy =
    fun (bytes: byte[]) ->
        let testCases = seq {
            for i = 0 to bytes.Length - 2 do
                for interesting in interesting16 do 
                    let origLE = uint16(bytes.[i]) + (uint16(bytes.[i + 1]) * 256us)
                    if not (couldBeBitflip(uint32(origLE), uint32(interesting))
                        || couldBeArith(uint32(origLE), uint32(interesting), 2uy)
                        || couldBeInterest(uint32(origLE), uint32(interesting), 2, false))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i] <- (uint8 interesting)
                        newBytes.[i + 1] <- (uint8 (interesting/256s))
                        yield newBytes
                    let interestingBE = swap16s interesting
                    if not ((interestingBE = interesting)
                        || couldBeBitflip(uint32(origLE), uint32(interestingBE))
                        || couldBeArith(uint32(origLE), uint32(interestingBE), 2uy)
                        || couldBeInterest(uint32(origLE), uint32(interestingBE), 2, true))
                    then 
                        let newBytes = Array.copy bytes
                        newBytes.[i] <- (uint8 interestingBE)
                        newBytes.[i + 1] <- (uint8 (interestingBE/256s))
                        yield newBytes
        }
        {
            Name = "interesting16"
            TestCasesPerExample = TestCasesPerByte (2 * (interesting16 |> Array.length))
            TestCases = testCases
        }
 
 
/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let private allStrategies(dictionaryValues: byte[][])= [ 
    bitFlip 1
    bitFlip 2
    bitFlip 4
    byteFlip 1
    byteFlip 2
    byteFlip 4
    arith8
    arith16
    arith32
    dictionary dictionaryValues 
    interest8
    interest16
]

let private applyStrategy (strategy: FuzzStrategy) (examples: TestCase list) (getSourceFile: TestCase -> string option) : seq<TestCase> = 
    seq {
        for example in examples do
            let stage = strategy(example.Data)
            for input in stage.TestCases do
                yield { example with Data = input; SourceFile = getSourceFile example; Stage = stage }
    }


let all (examples: TestCase list, dictionaryValues: byte[][]) : seq<TestCase> =     
    seq {
        yield applyStrategy useOriginalExample examples (fun example -> example.SourceFile)
        for strategy in allStrategies dictionaryValues do
            // fuzzed input doesn't come directly from a file, so return None for source file
            yield applyStrategy strategy examples (fun _ -> None)
    } |> Seq.collect id
