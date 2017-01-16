module DocumentDb

open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client

let client = new DocumentClient(Root.docDbUri, Root.docDbApiKey)

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
