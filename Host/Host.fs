module Host

open System
open System.Net

open FSharp.Data

open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client


// Connect to the DocumentDB Emulator running locally

module DocumentDb =

    let client = new DocumentClient(
                    new Uri("https://localhost:8081"), 
                    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
    
    let createDb (dbName:string) (client:DocumentClient) = 
        let db = new Database()
        db.Id <- dbName.ToLower()
        client.CreateDatabaseIfNotExistsAsync(db,new RequestOptions())
        |> Async.AwaitTask

    let createCollection (collectionName:string) (db: Database) (client:DocumentClient)= 
        let collection = new DocumentCollection()
        collection.Id<-collectionName.ToLower()
        client.CreateDocumentCollectionIfNotExistsAsync(db.CollectionsLink,collection,new RequestOptions())
        |> Async.AwaitTask

    let upsert (doc:obj) (collection:DocumentCollection) (client:DocumentClient) = 
        client.UpsertDocumentAsync(collection.DocumentsLink, doc)
        |> Async.AwaitTask

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
