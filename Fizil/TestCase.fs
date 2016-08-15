module TestCase


/// How many test cases do we expect per example 
type MaximumTestCasesPerExample =
 /// Most fuzz strategies produce one or more test cases per byte of example data
 | TestCasesPerByte    of int
/// Some fuzz strategies (useOriginal) produce one or more test cases per example
 | TestCasesPerExample of int


[<NoComparison>]
type Stage = {
    Name:                string
    TestCasesPerExample: MaximumTestCasesPerExample
    TestCases:           seq<byte[]>
}


[<NoComparison>]
type TestCase = {
    Data:          byte[]
    FileExtension: string
    /// SourceFile will be Some filename on an example file,
    /// None on a generated test case
    SourceFile:    string option
    Stage:         Stage
}
