module Org  

open Orleankka
open Orleankka.FSharp

open DocumentDb

open Newtonsoft.Json


type Id = string
type Email = string


type Contract =
    inherit IActor



type Commands = 
    | Create of Id*string*Email
    interface Contract

type Queries = 
    | GetState 
    interface Contract

type State = {
    id: Id
    name: string
    adminEmail: Email
    createdAt: System.DateTime
}
with 
    static member Zero =
        {
            id=""
            name=""
            adminEmail=""
            createdAt = System.DateTime.MinValue
        }

let handle state = 
    function 
    | Create (id,name,adminEmail) ->
        if state<> State.Zero then    
            failwith "already created"
        else
            {
                id=id
                name = name
                adminEmail = adminEmail
                createdAt = System.DateTime.Now
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
    let mutable s = State.Zero
    member this.Log = printfn "%s: %s" this.Id

    override this.Activate () = 
        this.Log "activating"
        task {
            let! doc = loadState this.Id
            this.Log "state loaded"
            match doc with
            | Some state ->
                s <- state
                this.Log "state updated"
            | None -> ignore() 
            // return null     
        }

    override this.Receive msg = task {
        sprintf "Handling msg %s" (msg.GetType().ToString())
        |> this.Log

        match msg with 
        | :? Commands as  cmd  -> 
            let state =  handle s cmd
            let! res = uploadState state
            s <- state
            return nothing
        | :? Queries as qry -> return response(s)
    }
