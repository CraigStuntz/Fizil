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


let private applyStrategy (strategy: FuzzStrategy) (examples: TestCase list) : seq<TestCase> = 
    seq {
        for example in examples do
            let stage = strategy(example.Data)
            for input in stage.TestCases do
                yield { example with Data = input; SourceFile = None; Stage = stage.Name }
    }


let all (examples: TestCase list) : seq<TestCase> =     
    seq {
        for strategy in FuzzStrategies.all do
            yield applyStrategy strategy examples
    } |> Seq.collect id
