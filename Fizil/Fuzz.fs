module Fuzz

open FuzzStrategies
open TestCase

let private inputs (example: TestCase) : seq<TestCase> = 
    seq {
        for strategy in FuzzStrategies.all do
            let stage = strategy(example.Data)
            for input in stage.TestCases do
                yield { example with Data = input; SourceFile = None; Stage = stage.Name }
    }


let all (examples: TestCase list) : seq<TestCase> = 
    examples |> Seq.collect inputs
