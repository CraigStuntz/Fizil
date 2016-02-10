module FuzzStrategies


open System


type FuzzStrategy = Byte[] -> seq<Byte[]>


let private flip (byte: Byte) (bitIndex: int) =
    (byte ^^^ (1uy <<< bitIndex))
     

let private bitFlip : FuzzStrategy = 
    fun (bytes: Byte[]) ->
        seq {
            for byte = 0 to bytes.Length - 1 do
                for bit in [0 .. 7] do
                    let newBytes = Array.copy bytes
                    let newByte = flip bytes.[byte] bit
                    newBytes.[byte] <- newByte
                    yield newBytes
        }

let all = [ bitFlip ]
                

