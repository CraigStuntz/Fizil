module FuzzStrategies

/// Takes a data example uses it as a seed to generate 0 or more new test cases
type FuzzStrategy = byte[] -> seq<byte[]>


let useOriginalExample : FuzzStrategy = 
    Seq.singleton


let private flip (byte: byte) (bitIndex: int) =
    (byte ^^^ (1uy <<< bitIndex))
     

let bitFlip : FuzzStrategy = 
    fun (bytes: byte[]) ->
        seq {
            for byte = 0 to bytes.Length - 1 do
                for bit in 0 .. 7 do
                    let newBytes = Array.copy bytes
                    let newByte = flip bytes.[byte] bit
                    newBytes.[byte] <- newByte
                    yield newBytes
        }


/// An ordered list of functions to use when starting with a single piece of 
/// example data and producing new examples to try
let all = [ 
    useOriginalExample
    bitFlip 
]
                

