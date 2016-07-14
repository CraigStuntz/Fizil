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
    let ``bitFlip returns one example for each bit``() = 
        let input = [| 0uy |]
        let fuzzed = FuzzStrategies.bitFlip input
        Assert.That(fuzzed |> Seq.length, Is.EqualTo 8)
        Assert.That(fuzzed, Contains.Item([| 1uy |]))
        Assert.That(fuzzed, Contains.Item([| 2uy |]))
        Assert.That(fuzzed, Contains.Item([| 4uy |]))
        Assert.That(fuzzed, Contains.Item([| 8uy |]))
        Assert.That(fuzzed, Contains.Item([| 16uy |]))
        Assert.That(fuzzed, Contains.Item([| 32uy |]))
        Assert.That(fuzzed, Contains.Item([| 64uy |]))
        Assert.That(fuzzed, Contains.Item([| 128uy |]))