module TestCase


type TestCase = {
    Data:          byte[]
    FileExtension: string
    /// SourceFile will be Some filename on an example file,
    /// None on a generated test case
    SourceFile:    string option
}
