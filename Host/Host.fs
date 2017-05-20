module Host

open System
open System.Net

open FSharp.Data

open Orleankka
open Orleankka.FSharp
open Orleankka.FSharp.Configuration
open Orleankka.FSharp.Runtime


module Pingers =
    type Message = 
    | Ping
    
    type Pinger() = 
        inherit Actor<Message>()

        override this.Receive msg = task{
            match msg with
            | Ping -> return response("Pong")
        }

open Pingers
open Wallets

[<EntryPoint>]
let main argv =
    printfn "starting actor system"
    
    let system = [|System.Reflection.Assembly.GetExecutingAssembly()|]
                |> ActorSystem.createPlayground
                |> ActorSystem.start   
    
    printfn "actor system started"

 
    let ping() = task {
        let pinger =  ActorSystem.actorOf<Pinger>(system,"myId")
        let! res = pinger <? Ping
        printfn "received: %s" res //Pong
    } 
    Task.run (ping) 
    |> ignore


    let transfer() = task {
        printfn "testing wallets"
        let petia = ActorSystem.actorOf<Wallet>(system, "petia")
        do! petia <! Deposit(10M)

        let! balance = petia <? GetBalance
        printfn "Petia balance is %M" balance


        let vasia = ActorSystem.actorOf<Wallet>(system, "vasia")
        do! petia <! TransferTo(vasia,5M)

        let! balance = vasia <? GetBalance

        printfn "Vasia balance is %M" balance
    }
    transfer |> Task.run |> ignore
    Console.ReadLine() |> ignore
    
    0 // return an integer exit code
