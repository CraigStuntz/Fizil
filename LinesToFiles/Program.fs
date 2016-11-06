[<EntryPoint>]
let main argv = 
    try
        System.IO.File.ReadAllLines("..\..\..\JSON_fuzzing.txt")
            |> Array.iteri (fun index line -> 
                let filename = index.ToString() + ".json"
                let fullPath = "..\\..\\..\\Demo\examples\\" + filename
                System.IO.File.WriteAllText(fullPath, line))
        0
    with 
        |  ex ->
            printf "%s" ex.Message
            System.Console.ReadLine() |> ignore
            -1