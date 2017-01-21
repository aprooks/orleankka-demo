module Org  

open Orleankka
open Orleankka.FSharp

open DocumentDb

open Newtonsoft.Json


type Id = string
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
    let mutable s = {id = ""; name=""; adminEmail  = "";createdAt = System.DateTime.Now}

    override this.Activate () = 
        task {
            let! doc = loadState this.Id
            match doc with
            | Some state ->
                s <- state
            | None -> ignore()            
        }

    override this.Receive msg = 
        task {
            match msg with 
            | Create (name,adminEmail) -> 
                if s.id <> "" then
                    failwith "cannot create twice"
                let! result = {
                                id = this.Id
                                name = name
                                adminEmail = adminEmail
                                createdAt = System.DateTime.Now
                                }
                                |> uploadState 
                return nothing 
    }

