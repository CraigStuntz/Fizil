module Fuzz

open System
open FuzzStrategies


let private inputs (example: Byte[]) : seq<Byte[]> = 
    seq {
        for fuzzStrategy in FuzzStrategies.all do
            for input in fuzzStrategy(example) do
                yield input
    }


let all (examples: Byte[] list) : seq<Byte[]> = 
    examples |> Seq.collect inputs
