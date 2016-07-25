module Convert


let toBytes(str: string) : byte[] =
    let bytes = Array.create (str.Length * sizeof<char>) 0uy
    System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
    bytes


let toString(bytes: byte[]) : string =
    let numChars = (bytes.Length / sizeof<char>) + (bytes.Length % sizeof<char>)
    let chars: char[] = Array.create numChars System.Char.MinValue
    System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length)
    System.String(chars)