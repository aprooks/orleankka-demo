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
    
    type Organization()=
        inherit Actor<Contract>()
        override this.Receive msg = task{
            match msg with 
            | Create (name,adminEmail) -> return nothing
        }

let TestDocumentDb ()= async {
    let! dbResponse = createDb "test" DocumentDb.client
    
    let! collection = createCollection "testcollection" dbResponse.Resource DocumentDb.client

    return! upsert {id="Hello1"; name="World"} collection.Resource DocumentDb.client
}

let ScaffoldGrains (system:IActorSystem) ()= task{
   let actor = ActorSystem.actorOf<Org.Organization>(system, "actor_id")
   
   do! actor <! Org.Create("test","admin@example.com")
}
open System.Threading.Tasks


[<EntryPoint>]
let main argv =
    printfn "running doc db tests"
    let res = TestDocumentDb() |> Async.RunSynchronously 
    res.Resource.Id    
    |> printfn "finished uploading: %A"

    printfn "starting actor system"
    
    use system = [|Assembly.GetExecutingAssembly()|]
                |> ActorSystem.createPlayground
                |> ActorSystem.start   
    
    printfn "actor system started"

    system
    |> ScaffoldGrains
    |> Task.run 
    |> ignore

    printfn "scaffolding complete"

    0 // return an integer exit code
