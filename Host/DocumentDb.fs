module DocumentDb

open System.Net

open Orleankka.FSharp

open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client

let client = new DocumentClient(Root.docDbUri, Root.docDbApiKey)

let createDb (dbName:string) (client:DocumentClient) = 
    let db = new Database()
    db.Id <- dbName.ToLower()
    client.CreateDatabaseIfNotExistsAsync(db,new RequestOptions())

let createCollection (collectionName:string) (db: Database) (client:DocumentClient)= 
    let collection = new DocumentCollection()
    collection.Id<-collectionName.ToLower()
    client.CreateDocumentCollectionIfNotExistsAsync(db.CollectionsLink,collection,new RequestOptions())

let upsert (doc:obj) (collection:DocumentCollection) (client:DocumentClient) = 
    client.UpsertDocumentAsync(collection.DocumentsLink, doc)

let loadDocument<'a> id (collection:DocumentCollection) (db:Database) (client:DocumentClient) = 
      task{
        try
            let! res = UriFactory.CreateDocumentUri(db.Id,collection.Id,id)
                    |> client.ReadDocumentAsync
            return res |> Some        
        with 
        | :? DocumentClientException as ex ->
                match ex.StatusCode.Value with 
                |  HttpStatusCode.NotFound -> return None
                | _ -> 
                    raise ex
                    return None
      }
