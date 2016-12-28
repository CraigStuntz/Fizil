open System
open StJson

[<EntryPoint>]
let main argv = 
    match argv with
    | [| path |] ->
        try
            let data = System.IO.File.ReadAllBytes path
        
            let p = StJsonParser(data |> List.ofArray)
            try
                let o = p.parse()

                match o with
                | Success _ -> 0
                | SyntaxError message -> 
                    eprintfn "%s" message
                    1
            with
                | ex -> 
                    printfn "%s" ex.Message
                    1
        with 
            | ex -> 
                printfn "*** CANNOT READ DATA AT %s" path
                printfn "%s" ex.Message
                1
    | _ -> 
        printfn "Usage: StJson.exe file.json"
        1