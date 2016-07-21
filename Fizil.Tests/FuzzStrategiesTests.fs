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
        let input = [| 0uy; 255uy |]
        let expected = [ 
            [| 1uy;   255uy |]
            [| 2uy;   255uy |]
            [| 4uy;   255uy |]
            [| 8uy;   255uy |]
            [| 16uy;  255uy |]
            [| 32uy;  255uy |]
            [| 64uy;  255uy |]
            [| 128uy; 255uy |] 
            [| 0uy;   254uy |]
            [| 0uy;   253uy |]
            [| 0uy;   251uy |]
            [| 0uy;   247uy |]
            [| 0uy;   239uy |]
            [| 0uy;   223uy |]
            [| 0uy;   191uy |]
            [| 0uy;   127uy |] 
        ]
        let fuzzed = FuzzStrategies.bitFlip 1 input
        let actual = List.ofSeq fuzzed
        Assert.That(actual, Is.EqualTo expected)
