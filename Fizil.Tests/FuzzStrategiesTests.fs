namespace Fizil.Tests

open NUnit.Framework

module FuzzStrategiesTest = 

    [<Test>]
    let ``useOriginalExample returns originals``() = 
        let input = [| 0uy |]
        let fuzzed = FuzzStrategies.useOriginalExample input
        Assert.That(fuzzed |> Seq.length, Is.EqualTo 1)
        Assert.That(fuzzed |> Seq.head |> Seq.head, Is.EqualTo (input |> Array.head))


    [<Test>]
    let ``bitFlip1 flips l bit``() = 
        let input = [| 0b00000000uy; 0b11111111uy |]
        let expected = [ 
            [| 0b00000001uy; 0b11111111uy |]
            [| 0b00000010uy; 0b11111111uy |]
            [| 0b00000100uy; 0b11111111uy |]
            [| 0b00001000uy; 0b11111111uy |]
            [| 0b00010000uy; 0b11111111uy |]
            [| 0b00100000uy; 0b11111111uy |]
            [| 0b01000000uy; 0b11111111uy |]
            [| 0b10000000uy; 0b11111111uy |] 
            [| 0b00000000uy; 0b11111110uy |]
            [| 0b00000000uy; 0b11111101uy |]
            [| 0b00000000uy; 0b11111011uy |]
            [| 0b00000000uy; 0b11110111uy |]
            [| 0b00000000uy; 0b11101111uy |]
            [| 0b00000000uy; 0b11011111uy |]
            [| 0b00000000uy; 0b10111111uy |]
            [| 0b00000000uy; 0b01111111uy |] 
        ]
        let fuzzed = FuzzStrategies.bitFlip 1 input
        let actual = List.ofSeq fuzzed
        Assert.That(actual, Is.EqualTo expected)
