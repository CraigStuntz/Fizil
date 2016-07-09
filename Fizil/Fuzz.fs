module Fuzz

open FuzzStrategies
open TestCase

let private inputs (example: TestCase) : seq<TestCase> = 
    seq {
        for fuzzStrategy in FuzzStrategies.all do
            for input in fuzzStrategy(example.Data) do
                yield { example with Data = input; SourceFile = None }
    }


let all (examples: TestCase list) : seq<TestCase> = 
    examples |> Seq.collect inputs
