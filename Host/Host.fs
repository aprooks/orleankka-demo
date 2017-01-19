module Host

open System
open System.Net

open FSharp.Data

open DocumentDb

open Newtonsoft.Json

type Id = string

type TestDoc ={
    id: Id
    name: string
}

open Orleankka
open Orleankka.FSharp
open Orleankka.FSharp.Configuration
open Orleankka.FSharp.Runtime
open System.Reflection



module Org = 

    type Email = string

    type Contract = 
    | Create of string*Email

    type State = {
        id: Id
        name: string
        adminEmail: Email
        createdAt: System.DateTime
    }
    
    let loadState id = task {
            let! db = createDb "actors" DocumentDb.client
            let! coll = createCollection "states" db.Resource DocumentDb.client
            let! state = loadDocument id coll.Resource db.Resource DocumentDb.client
            return match state with
                   | Some doc -> 
                                 JsonConvert.DeserializeObject<State>(doc.Resource.ToString())
                                 |> Some
                   | None -> None 
    }

    let uploadState state = task{
            let! db = createDb "actors" DocumentDb.client
            let! coll = createCollection "states" db.Resource DocumentDb.client
            return! upsert state coll.Resource DocumentDb.client
    }

    type Organization()=
        inherit Actor<Contract>()
        let mutable s = {id = ""; name=""; adminEmail  = "";createdAt = DateTime.Now}

        override this.Activate () = 
            printfn "activated with base.id %s" base.Id
            let id = base.Id
            task {
                printfn "activated with %s" id
                let! doc = loadState id
                match doc with
                | Some state ->
                    s <- state
                | None -> ignore()            
            }

        override this.Receive msg = 
            printfn "recieved msg of %s" (msg.GetType().ToString())
            let id = base.Id
            task {
                match msg with 
                | Create (name,adminEmail) -> 
                    if s.id <> "" then
                        failwith "cannot create twice"

                    let! result = {
                                            id = id
                                            name = name
                                            adminEmail = adminEmail
                                            createdAt = System.DateTime.Now
                                    }
                                    |> uploadState 
                    return nothing 
        }

let TestDocumentDb ()= task {
    let! dbResponse = createDb "test" DocumentDb.client
    
    let! collection = createCollection "testcollection" dbResponse.Resource DocumentDb.client

    let! upsertResult = upsert {id="Hello1"; name="World"} collection.Resource DocumentDb.client
    printfn "upserted doc %A" upsertResult.Resource.Id   

    let! existingDoc = loadDocument "Hello1" collection.Resource dbResponse.Resource DocumentDb.client
    match existingDoc with 
    | Some d -> printfn "existingDoc loaded %A" d.Resource
    | None -> printfn "failed to load doc"
    
    let! nonExistingDoc = loadDocument "Hello2" collection.Resource dbResponse.Resource DocumentDb.client
    match nonExistingDoc with 
    | Some d -> printfn "existingDoc loaded %A" d.Resource
    | None -> printfn "failed to load doc"

    return nothing
}

let ScaffoldGrains (system:IActorSystem) ()= task{
   let actor = ActorSystem.actorOf<Org.Organization>(system, "actor_id")
   
   do! actor <! Org.Create("test","admin@example.com")
}
open System.Threading.Tasks

[<EntryPoint>]
let main argv =
    printfn "running doc db tests"
    let res = TestDocumentDb |> Task.run 
    match res with
    | Choice1Of2 d -> printfn "finished documentdb upload"
    | Choice2Of2 exn -> printfn "exception running docDb upload\n %s" exn.Message

    printfn "starting actor system"
    
    use system = [|Assembly.GetExecutingAssembly()|]
                |> ActorSystem.createPlayground
                |> ActorSystem.start   
    
    printfn "actor system started"

    let res = 
        system
        |> ScaffoldGrains
        |> Task.run 
    match res with 
    | Choice1Of2 _ -> printfn "scaffolding complete"
    | Choice2Of2 exn -> printfn "exeption on scaffolding:\n %s\n %s" exn.Message exn.StackTrace

    0 // return an integer exit code
