module Host

open System
open System.Net

open FSharp.Data

open DocumentDb

type Id = string

type TestDoc ={
    id: Id
    name: string
}

let Scaffold ()= async {
    let! dbResponse = createDb "test" DocumentDb.client
    
    let! collection = createCollection "testcollection" dbResponse.Resource DocumentDb.client

    return! upsert {id="Hello1"; name="World"} collection.Resource DocumentDb.client
}


[<EntryPoint>]
let main argv =
    printfn "running scaffold"
    let res = Scaffold() |> Async.RunSynchronously 
    res.Resource.Id    
    |> printfn "finished uploading: %A"
    0 // return an integer exit code
