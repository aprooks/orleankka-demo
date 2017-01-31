module State

open DocumentDb

open Newtonsoft.Json

open Orleankka.FSharp

let loadState<'a> id = task {
        let! db = createDb "actors" DocumentDb.client
        let! coll = createCollection Root.statesCollection db.Resource DocumentDb.client
        let! state = loadDocument id coll.Resource db.Resource DocumentDb.client
        return match state with
                | Some doc -> 
                                JsonConvert.DeserializeObject<'a>(doc.Resource.ToString())
                                |> Some
                | None -> None 
}

let uploadState state = task {
        let! db = createDb "actors" DocumentDb.client
        let! coll = createCollection Root.statesCollection db.Resource DocumentDb.client
        let! result = upsert state coll.Resource DocumentDb.client
        return state
}