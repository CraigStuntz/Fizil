// This code is directly translated from STJSON ( https://github.com/nst/STJSON )
// under terms of the MIT license. 
// It is not idiomatic F#

namespace StJson

open System

[<Flags>]
type Options = 
| none = 0
| useUnicodeReplacementCharacter = 1


type Scalar = | UnicodeScalar of byte


type JsonValue = 
| JsonBool   of bool
| JsonNull
| JsonNumber of string
| JsonString of string
| JsonArray  of JsonValue[]
| JsonObject of Map<string, JsonValue>


type JsonParseResult =
| Success of JsonValue
| SyntaxError of string


type ASCIIByte =
| objectOpen = 0x7Buy     // {
| tab = 0x09uy            // \t
| newline = 0x0Auy        // \n
| space = 0x20uy          // " "
| carriageReturn = 0x0Duy // \r
| doubleQuote = 0x22uy    // "
| colon = 0x3Auy      // :
| objectClose = 0x7Duy    // }
| comma = 0x2Cuy          // ,
| arrayOpen = 0x5Buy      // [
| arrayClose = 0x5Duy     // ]
| slash = 0x2Fuy          // /
| backSlash = 0x5Cuy      // \
| plus = 0x2Buy           // +
| minus = 0x2Duy          // -
| dot = 0x2Euy            // .
| zero = 0x30uy           // 0
| one = 0x31uy            // 1
| nine = 0x39uy           // 9
| utf8BOMByte1 = 0xEFuy
| utf8BOMByte2 = 0xBBuy
| utf8BOMByte3 = 0xBFuy
| A = 0x41uy
| E = 0x45uy
| F = 0x46uy
| Z = 0x5Auy
| a = 0x61uy
| z = 0x7Auy
| e = 0x65uy
| f = 0x66uy
| l = 0x6Cuy
| n = 0x6euy
| r = 0x72uy
| s = 0x73uy
| t = 0x74uy
| u = 0x75uy


type JSONError = 
| CannotReadByte of int
| ExpectedDigit of int
| ExpectedCharacterToBeUnescaped of int
| ExpectedValue of int
| ExpectedString of int
| ExpectedNumber of int
| ExpectedColon of int
| ExpectedObjectOpen of int
| ExpectedObjectContent of int
| ExpectedObjectClose of int
| ExpectedArrayOpen of int
| ExpectedArrayClose of int
| ExpectedDoubleQuote of int
| ExpectedCharacter of int
| CannotBuildStringFromData of int
| ExpectedAcceptableCodepointOrEscapedSequence of (int * byte[])
| CannotReadInt of int
| ExtraData of int
| MaxParserDepthReached of int
| FoundDigitAfterLeadingZero of int
| FoundGarbage of int
| ExpectedHighSurrogate of int
| ExpectedLowSurrogate of int
| FoundSurrogatesWithInvalidCodepoint of int
| FoundBOMForUnsupportdEncodingUTF16BE
| FoundBOMForUnsupportdEncodingUTF16LE
| FoundBOMForUnsupportdEncodingUTF32BE
| FoundBOMForUnsupportdEncodingUTF32LE


type StJsonParser(data: byte list, ?maxParserDepth: int, ?options: Options) = 

    let maxParserDepth = defaultArg maxParserDepth 500
    let options = defaultArg options Options.none
    let mutable i = 0
    let dataLength = data |> List.length
    let mutable parserDepth = 0
    let REPLACEMENT_STRING = "\u{FFFD}"
    let utf8Encoding = System.Text.UTF8Encoding(false, true)

    let throw(errorType: JSONError) = invalidOp <| sprintf "%A" errorType

    let toString(bytes: byte list) : string =
        let numChars = (bytes.Length / sizeof<char>) + (bytes.Length % sizeof<char>)
        let chars: char[] = Array.create numChars System.Char.MinValue
        System.Buffer.BlockCopy((Array.ofList bytes), 0, chars, 0, bytes.Length)
        System.String(chars)


    new (data: string, ?maxParserDepth: int, ?options: Options) = 
        let bytes = System.Text.Encoding.UTF8.GetBytes(data) |> List.ofArray
        let maxParserDepth = defaultArg maxParserDepth 500
        let options = defaultArg options Options.none
        StJsonParser(bytes, maxParserDepth, options)

    member this.printRemainingString : String =
        let _, remainingData = List.splitAt i data 
        sprintf "-- REMAINING STRING FROM %d: %s" i (remainingData |> toString)


    override this.ToString() : String =
        data |> toString

    
    member this.read() : byte option =
        match i < dataLength with
        | true  -> 
            Some data.[i]
        | false ->
            // printfn "can't read at index %d" i 
            None

    
    member this.readAndMoveByteEither(expected: ASCIIByte list) : Scalar option =      
        match this.read() with
        | None -> 
            None
        | Some readByte -> 
            let rec loop(remainingExpected: ASCIIByte list) =
                match remainingExpected with
                | [] -> None
                | b :: _rest when (byte b) = readByte -> 
                    i <- i + 1
                    Some (UnicodeScalar readByte)
                | _ :: rest -> loop rest
            loop expected

    
    member this.readAndMove(b: ASCIIByte) : bool =
        match this.read() with
        | Some readByte when (byte b) = readByte -> 
            i <- i + 1
            true
        | Some _
        | None   -> false

    
    member this.readAndMoveInByteRange(from: ASCIIByte, top: ASCIIByte) : char option =
        match this.read() with
        | Some readByte when readByte >= (byte from) && readByte <= (byte top) ->
            i <- i + 1
            Some (Convert.ToChar readByte)
        | Some _ 
        | None   -> None

    
    member this.readAndMoveHexadecimalDigit() : byte option =
        match this.read() with
        | Some readByte when this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) |> Option.isSome -> Some readByte
        | Some readByte when this.readAndMoveInByteRange(ASCIIByte.A,    ASCIIByte.F)    |> Option.isSome -> Some readByte
        | Some readByte when this.readAndMoveInByteRange(ASCIIByte.a,    ASCIIByte.f)    |> Option.isSome -> Some readByte
        | Some _
        | None   -> None
    

    member this.readAndMoveWhitespace() : bool =
        this.readAndMove(ASCIIByte.tab)
            || this.readAndMove(ASCIIByte.newline)
            || this.readAndMove(ASCIIByte.carriageReturn)
            || this.readAndMove(ASCIIByte.space)
    

    member this.readAndMoveAcceptableByte() : byte option =
        match this.read() with
        | Some readByte when 
            readByte <> (byte ASCIIByte.doubleQuote) 
            && readByte <> (byte ASCIIByte.backSlash) 
            && readByte > 0x1Fuy  
                 -> 
                    i <- i + 1
                    Some readByte
        | Some _
        | None   -> None

    
    member private this.bypassWhitespace() =
        match this.readAndMoveWhitespace() with
        | true  -> this.bypassWhitespace()
        | false -> ()


    member this.readArray() : JsonValue[] =
        do this.bypassWhitespace()
        
        match this.readAndMove(ASCIIByte.arrayOpen) with
        | false -> throw (JSONError.ExpectedArrayOpen i)
        | true ->
            parserDepth <- parserDepth + 1
            if parserDepth >= maxParserDepth 
            then throw (JSONError.MaxParserDepthReached parserDepth)

        do this.bypassWhitespace()
        
        match this.readAndMove(ASCIIByte.arrayClose) with
        | true ->
            parserDepth <- parserDepth - 1
            [||]
        | false ->     
            let rec readArrayValues() : JsonValue list =
                let jsonValue = this.readValue()
                do this.bypassWhitespace()
                match this.readAndMove(ASCIIByte.comma) with
                | true ->
                    do this.bypassWhitespace()
                    jsonValue :: readArrayValues()
                | false ->
                    do this.bypassWhitespace()
                    match this.readAndMove(ASCIIByte.arrayClose) with
                    | true  -> []
                    | false -> throw (JSONError.ExpectedArrayClose i)
            let values = readArrayValues() |> List.rev |> Array.ofList
            parserDepth <- parserDepth - 1
            values

    
    /// return unescaped value of "\{byte}". the `\` has already been read at this pont
    member this.unescape(byte: uint8) : uint8 =
        match byte with
        | b when b = (uint8 ASCIIByte.backSlash)   -> byte
        | b when b = (uint8 ASCIIByte.doubleQuote) -> byte
        | b when b = (uint8 ASCIIByte.slash)       -> byte
        | b when b =  System.Convert.ToByte('b')   -> 0x08uy // \b backspace
        | b when b =  System.Convert.ToByte('f')   -> 0x0Cuy // \f form feed
        | b when b =  System.Convert.ToByte('n')   -> 0x0Auy // \n line feed
        | b when b =  System.Convert.ToByte('r')   -> 0x0Duy // \r carriage return
        | b when b =  System.Convert.ToByte('t')   -> 0x09uy // \t tabulation
        | _ -> throw (JSONError.ExpectedCharacterToBeUnescaped i)


    member this.isValidCodepoint(cp: int) : bool =
        // http://www.unicode.org/versions/Unicode5.2.0/ch03.pdf
        // Table 3-7. Well-Formed UTF-8 Byte Sequences
        (cp >= 0 && cp <= 0xD7FF) || (cp >= 0xE000 && cp <= 0x10FFFF)
    

    member this.isHighSurrogate(cp: int) : bool =
        cp >= 0xD800 && cp <= 0xDBFF

    
    member this.isLowSurrogate(cp: int) : bool =
        cp >= 0xDC00 && cp <= 0xDFFF

    
    member this.readAndMoveUEscapedCodepoint() : (string * int) option = 
        match this.readAndMove(ASCIIByte.u) with
        | false -> None
        | true -> 
            match this.readAndMoveHexadecimalDigit() with
            | Some b1 ->
                match this.readAndMoveHexadecimalDigit() with
                | Some b2 ->
                    match this.readAndMoveHexadecimalDigit() with
                    | Some b3 -> 
                        match this.readAndMoveHexadecimalDigit() with
                        | Some b4 ->        
                            let s = System.String(utf8Encoding.GetChars([|b1; b2; b3; b4|]))
                            Some (s, (System.Int32.Parse(s, System.Globalization.NumberStyles.HexNumber)))
                        | None -> None
                    | None -> None
                | None -> None
            | None -> None

    
    member this.readAndMoveEscapedCodepointOrSurrogates() : string option =
        
        let useReplacementString = options &&& Options.useUnicodeReplacementCharacter = Options.useUnicodeReplacementCharacter
        
        (*
         http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-262.pdf
         -> 6.1.4 The String Type
         // A code unit in the range 0 to 0xD7FF or in the range 0xE000 to 0xFFFF is interpreted as a code point with the same value.
         * A sequence of two code units, where the first code unit c1 is in the range 0xD800 to 0xDBFF and the second code unit c2
         is in the range 0xDC00 to 0xDFFF, is a surrogate pair and is interpreted as a code point with the value (c1 ‑ 0xD800) ×
         0x400 + (c2 ‑ 0xDC00) + 0x10000. (See 10.1.2)
         * A code unit that is in the range 0xD800 to 0xDFFF, but is not part of a surrogate pair, is interpreted as a code point with
         the same value.
         *)
        
        match this.readAndMoveUEscapedCodepoint() with
        | None -> None
        | Some (s1, c1) ->
            match this.isValidCodepoint c1 with
            | true ->
                // valid codepoint -> return
                Some (sprintf "\(%c)" (System.Convert.ToChar c1))
            | false ->       
                // invalid codepoint must be high surrogate, or error
                match this.isHighSurrogate c1 with
                | false ->
                    match useReplacementString with
                    | true -> Some REPLACEMENT_STRING 
                    | false -> Some (sprintf "\\u%s" s1)
                | true ->
                    // look for second surrogate escape character, or error
                    match this.readAndMove(ASCIIByte.backSlash) with
                    | false ->
                        match useReplacementString with
                        | true -> Some REPLACEMENT_STRING 
                        | false -> Some (sprintf "\\u%s" s1)
                    | true ->
                        // read second codepoint
                        match this.readAndMoveUEscapedCodepoint() with 
                        | None -> 
                            // or escaped sequence
                            let x = this.readAndMoveEscapedSequence()
                            match useReplacementString with
                            | true -> Some (sprintf "%s%s" REPLACEMENT_STRING x)
                            | false -> Some (sprintf "\\u%s%s" s1 x)
                        | Some (s2, c2) ->
                            // second codepoint must be low surrogate, or error
                            match this.isLowSurrogate(c2) with
                            | false -> 
                                match useReplacementString with
                                | true -> Some (REPLACEMENT_STRING + REPLACEMENT_STRING)
                                | false -> Some (sprintf "\\u%s\\u%s" s1 s2)
                            | true ->         
                                let finalCodepoint = 0x400 + 0x2400 + (c1 - 0xD800) + (c2 - 0xDC00) + 0x10000
                                match this.isValidCodepoint(finalCodepoint) with
                                | false -> 
                                    match useReplacementString with
                                    | true -> Some (REPLACEMENT_STRING + REPLACEMENT_STRING)
                                    | false -> Some (sprintf "\\u%s\\u%s" s1 s2)        
                                | true -> Some (sprintf "\(%s)" ( System.Char.ConvertFromUtf32 finalCodepoint))

    
    member this.readAndMoveEscapedSequence() : string =
        match this.readAndMoveEscapedCodepointOrSurrogates() with
        | Some s -> s
        | None -> 
            match this.read() with
            | None -> throw (JSONError.ExpectedCharacter i)
            | Some b ->        
                i <- i + 1
                let unescaped = this.unescape(b)
                utf8Encoding.GetString([| unescaped |])

    
    member this.readString() : string =
        match this.readAndMove(ASCIIByte.doubleQuote) with 
        | false -> throw (JSONError.ExpectedDoubleQuote i)
        | true -> 
            let rec loop() : string = 
                match this.readAndMove(ASCIIByte.doubleQuote) with
                | true -> ""
                | false ->
                    match this.read() with
                    | None -> throw (JSONError.ExpectedCharacter i)
                    | Some _ ->
                        match this.readAndMove(ASCIIByte.backSlash) with
                        | true ->               
                            let x = this.readAndMoveEscapedSequence()
                            x + loop()
                        | false ->
                            let rec readAndMoveAcceptableBytes() : byte list = 
                                match this.readAndMoveAcceptableByte() with
                                | Some b -> b :: readAndMoveAcceptableBytes()
                                | None -> []
                            let acceptableBytes = 
                                readAndMoveAcceptableBytes() 
                                |> Array.ofList 
                            try
                                let s = utf8Encoding.GetString(acceptableBytes)   
                                match s with 
                                | "" -> throw (JSONError.ExpectedAcceptableCodepointOrEscapedSequence (i, acceptableBytes))   
                                | _ -> s + loop()
                            with
                            | :? System.Text.DecoderFallbackException -> 
                                throw (JSONError.ExpectedAcceptableCodepointOrEscapedSequence (i, acceptableBytes))
            loop()
        
           
    member this.readObject() : Map<string, JsonValue> =
        match this.readAndMove(ASCIIByte.objectOpen) with
        | false -> throw (JSONError.ExpectedObjectOpen i)
        | true ->
            do this.bypassWhitespace()
            match this.readAndMove(ASCIIByte.objectClose) with
            | true -> Map.empty
            | false -> 
                parserDepth <- parserDepth + 1
                if parserDepth > maxParserDepth 
                then throw (JSONError.MaxParserDepthReached parserDepth)
        
                let rec readObjectProperties (accum: Map<string, JsonValue>) : Map<string, JsonValue> =
                    do this.bypassWhitespace()
        
                    let s = this.readString()
            
                    do this.bypassWhitespace()
            
                    match this.readAndMove(ASCIIByte.colon) with
                    | false -> throw (JSONError.ExpectedColon i)
                    | true -> 
                        let v = this.readValue() 
                        do this.bypassWhitespace()
                        match this.readAndMove(ASCIIByte.comma) with
                        | true -> readObjectProperties (accum |> Map.add s v)
                        | false -> 
                            match this.readAndMove(ASCIIByte.objectClose) with
                            | false -> throw (JSONError.ExpectedObjectClose i)
                            | true -> (accum |> Map.add s v)
                let readObject = readObjectProperties Map.empty
                parserDepth <- parserDepth - 1
                readObject

    
    member this.throwIfStartsWithUTF16BOM() =
        let BOM_LENGTH = 2       
        if dataLength >= BOM_LENGTH
        then
            let UTF_16_BE = [|0xFEuy; 0xFFuy|]
            let UTF_16_LE = [|0xFFuy; 0xFEuy|]
            let BOM = [| data.[0]; data.[1] |]
            if BOM = UTF_16_BE
            then throw JSONError.FoundBOMForUnsupportdEncodingUTF16BE
            if BOM = UTF_16_LE 
            then throw JSONError.FoundBOMForUnsupportdEncodingUTF16LE

    
    member this.throwIfStartsWithUTF32BOM() =
        let BOM_LENGTH = 4
        if dataLength >= BOM_LENGTH
        then
            let UTF_32_BE = [|0x00uy; 0x00uy; 0xFEuy; 0xFFuy|]
            let UTF_32_LE = [|0xFFuy; 0xFEuy; 0x00uy; 0x00uy|]
            let BOM = [| data.[0]; data.[1]; data.[2]; data.[3] |]
            if BOM = UTF_32_BE
            then throw JSONError.FoundBOMForUnsupportdEncodingUTF32BE
            if BOM = UTF_32_LE 
            then throw JSONError.FoundBOMForUnsupportdEncodingUTF32LE

    
    member this.parse() : JsonParseResult =
        
        // throw if a UTF-16 or UTF-32 BOM is found
        // this is the only place where STJSON does not follow RFC 7159
        // which supports UTF-16 anf UTF-32, optionnaly preceeded by a BOM
        // STJSON only supports UTF-8
        do this.throwIfStartsWithUTF16BOM()
        do this.throwIfStartsWithUTF32BOM()
        
        try
            let parseJson() = 
                let o = this.readValue()       
                do this.bypassWhitespace()
                match this.read() with
                | None -> o
                | Some _ -> throw (JSONError.ExtraData i)
            // skip UTF-8 BOM if present (EF BB BF)
            if this.readAndMove(ASCIIByte.utf8BOMByte1) 
            then
                if this.readAndMove(ASCIIByte.utf8BOMByte2)
                then 
                    if this.readAndMove(ASCIIByte.utf8BOMByte3)
                    then
                        Success (parseJson())
                    else SyntaxError "Expected UTF-8 BOM byte 3"
                else SyntaxError "Expected UTF-8 BOM byte 2"
            else Success (parseJson())
        with
        | :? System.InvalidOperationException as ex -> SyntaxError ex.Message
                

    member this.readValue() : JsonValue =
        
        do this.bypassWhitespace()
        
        match this.read() with
        | None -> throw (JSONError.CannotReadByte i)
        | Some byte ->
            match byte with 
            | x when x = (uint8 ASCIIByte.arrayOpen) ->
                JsonArray (this.readArray())
            | x when x = (uint8 ASCIIByte.doubleQuote) ->
                JsonString (this.readString())
            | x when x = (uint8 ASCIIByte.objectOpen) ->
                JsonObject (this.readObject())
            | x when x = (uint8 ASCIIByte.t) ->
                let startPos = i
                if this.readAndMove(ASCIIByte.t)
                    && this.readAndMove(ASCIIByte.r)
                    && this.readAndMove(ASCIIByte.u)
                    && this.readAndMove(ASCIIByte.e)
                then JsonBool true
                else throw (JSONError.FoundGarbage startPos)
            | x when x = (uint8 ASCIIByte.f) ->
                let startPos = i
                if this.readAndMove(ASCIIByte.f)
                    && this.readAndMove(ASCIIByte.a)
                    && this.readAndMove(ASCIIByte.l)
                    && this.readAndMove(ASCIIByte.s)
                    && this.readAndMove(ASCIIByte.e)
                then JsonBool false
                else throw (JSONError.FoundGarbage startPos)
            | x when x = (uint8 ASCIIByte.n) ->
                let startPos = i
                if this.readAndMove(ASCIIByte.n)
                    && this.readAndMove(ASCIIByte.u)
                    && this.readAndMove(ASCIIByte.l)
                    && this.readAndMove(ASCIIByte.l)
                then JsonNull
                else throw (JSONError.FoundGarbage startPos)
            | _ ->
                match this.readNumber() with
                | None -> throw (JSONError.ExpectedValue i)
                | Some str -> JsonNumber str

    
    /// Returns number as string since JSON's numbers are different than F#'s numbers
    /// and we don't actually care about the value. However, the important thing is we
    /// return None for something which doesn't look like a number
    member this.readNumber() : string option =
        // State machine per ECMA spec
        let rec startState() =
            match this.readAndMove(ASCIIByte.minus) with 
            | true  -> maybeLeadingZeroState "-" 
            | false -> maybeLeadingZeroState ""
        and maybeLeadingZeroState(accum: string) = 
            match this.readAndMove(ASCIIByte.zero) with
            | true  -> maybeDecimalOrExponentAfterLeadingZero (accum + "0")
            | false -> integerState(accum)
        and maybeDecimalOrExponentAfterLeadingZero(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.one, ASCIIByte.nine) with
            | Some _ -> throw (JSONError.FoundDigitAfterLeadingZero i)
            | None ->
                match this.readAndMove(ASCIIByte.dot) with
                | true  -> fractionState(accum + ".") 
                | false -> 
                    tryReadExponentState accum
        and maybeDecimalState(accum: string) = 
            match this.readAndMove(ASCIIByte.dot) with
            | true ->  fractionState (accum + ".")
            | false -> Some accum
        and integerState(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.one, ASCIIByte.nine) with
            | Some c ->
                remainingIntegerState(accum + c.ToString())
            | None   -> throw (JSONError.ExpectedDigit i)
        and remainingIntegerState(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) with
            | Some c ->
                remainingIntegerState(accum + c.ToString())
            | None   ->
                match this.readAndMove(ASCIIByte.dot) with
                | true ->  fractionState (accum + ".")
                | false -> tryReadExponentState accum
        and fractionState(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) with
            | Some c ->
                remainingFractionState(accum + c.ToString())
            | None   -> throw (JSONError.ExpectedDigit i)
        and remainingFractionState(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) with
            | Some c ->
                remainingFractionState(accum + c.ToString())
            | None   -> tryReadExponentState accum
        and tryReadExponentState(accum: string) =
            match this.readAndMove(ASCIIByte.E) with 
            | true -> tryReadExponentPlusMinusState (accum + "E")
            | false ->
                match this.readAndMove(ASCIIByte.e) with
                | true -> tryReadExponentPlusMinusState (accum + "e")
                | false -> Some accum
        and tryReadExponentPlusMinusState(accum: string) =
            match this.readAndMove(ASCIIByte.plus) with 
            | true -> readExponentDigitsState (accum + "+")
            | false ->
                match this.readAndMove(ASCIIByte.minus) with
                | true -> readExponentDigitsState (accum + "-")
                | false -> readExponentDigitsState accum
        and readExponentDigitsState(accum: string) =
            match this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) with
            | Some c ->
                remainingExponentDigitsState(accum + c.ToString())
            | None   -> throw (JSONError.ExpectedDigit i)
        and remainingExponentDigitsState(accum: string) = 
            match this.readAndMoveInByteRange(ASCIIByte.zero, ASCIIByte.nine) with
            | Some c ->
                remainingExponentDigitsState(accum + c.ToString())
            | None   ->
                Some accum

        startState()