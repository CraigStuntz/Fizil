module Convert

open System


let toBytes(str: string) : Byte[] =
    let bytes = Array.create (str.Length * sizeof<char>) 0uy
    System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
    bytes


let toString(bytes: Byte[]) : string =
    let chars: Char[] = Array.create (bytes.Length / sizeof<char>) Char.MinValue
    System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length)
    System.String(chars)

