module Wallets

open Orleankka.FSharp


type WalletId = string

type Message=
    | Deposit of decimal
    | Withdraw of decimal
    | TransferTo of ActorRef<obj> * decimal
    | GetBalance

type Wallet() = 
    inherit Actor<Message>()
    let mutable balance = 0M 

    override this.Receive msg = task {
        printfn "Received msg"
        match msg with
        | Deposit amnt->
            balance <- amnt + balance
            return nothing

        | TransferTo (walletId, amnt) ->
            balance <- amnt - balance 
            do! walletId <! Deposit(amnt)
            return nothing
        | GetBalance -> return response(balance)
    }
