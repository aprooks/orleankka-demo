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
    | Disable
    | ConfirmRegistration
    interface Contract

type Queries = 
    | GetState 
    interface Contract

type State = {
    id: Id
    name: string
    adminEmail: Email
    disabled: bool
    createdAt: System.DateTime
    registrationConfirmed:bool
}
with 
    static member Zero =
        {
            id=""
            name=""
            adminEmail=""
            disabled = false
            registrationConfirmed = false
            createdAt = System.DateTime.MinValue
        }

let handle state = 
    function 
    | Create (id,name,adminEmail) ->
        if state<> State.Zero then    
            failwith "already created"
        else
            {
                State.Zero with
                    id=id
                    name = name
                    adminEmail = adminEmail
                    createdAt = System.DateTime.Now
            }
    | ConfirmRegistration ->         
        { state with registrationConfirmed = true}
    | Disable -> 
        if state.registrationConfirmed then
            state
        else
            { state with disabled = true }



open Orleankka.Services
open Orleankka.FSharp.Configuration
open State

let runDisactivationReminder (reminders:IReminderService ) command state = task {
    match command with 
    | Create _ ->
        do! reminders.Register("deactivate",System.TimeSpan.FromMinutes(5.),System.TimeSpan.FromMinutes(5.)) |> Task.awaitTask
                
    | Disable | ConfirmRegistration ->
        let! registered = reminders.IsRegistered("deactivate")
        match registered with 
        | true -> do! reminders.Unregister("deactivate") |> Task.awaitTask
        | _ -> ignore()
    return state
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
        }
    override this.OnReminder reminderId =
        task {
            sprintf "Handling reminder %s" reminderId 
            |> this.Log
            let actor = ActorSystem.actorOf<Organization>(this.System,this.Id)
            do! actor <! Disable
        }
        :> System.Threading.Tasks.Task

    override this.Receive msg = task {
        sprintf "Handling msg %s" (msg.GetType().ToString())
        |> this.Log

        match msg with 
        | :? Commands as  cmd  -> 
            let state =  handle s cmd
            let! res = uploadState state
            let! timerRes = runDisactivationReminder this.Reminders cmd state
            s <- state
            return nothing
        | :? Queries as qry -> return response(s)
        |_-> 
            failwith "message type not supported" 
            return nothing
    }
