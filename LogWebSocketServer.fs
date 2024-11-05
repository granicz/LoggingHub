module LogWebSocketServer

open WebSharper
open WebSharper.AspNetCore.WebSocket.Server

type [<JavaScript>] C2SMessage =
    | Ping

and [<JavaScript>] S2CMessage =
    | LogMessage of i:int * msg:string
    | Fatal of msg:string
    | Pong

let Clients : (string * WebSocketClient<S2CMessage, C2SMessage>) list ref = ref []

let LogCounter = ref 0

(*
 * Send a log message to all listening clients. 
 *)
let Log (msg: string) =
    LogCounter.Value <- LogCounter.Value + 1
    Clients.Value
    |> List.iter (fun (ip, client: WebSocketClient<S2CMessage, C2SMessage>) ->
        client.Post (S2CMessage.LogMessage(LogCounter.Value, msg))
    )

(*
 * Start the log websocket agent.
 *)
let LoggingHub(): Agent<S2CMessage, C2SMessage> =
    /// print to debug output and stdout
    let dprintfn x =
        Printf.ksprintf (fun s ->
            System.Diagnostics.Debug.WriteLine s
            stdout.WriteLine s
        ) x

    fun client -> async {
        // Register new client starting it
        let clientIP =
            client.Connection.Context.Connection.RemoteIpAddress.ToString()
        Clients.Value <- (clientIP, client) :: Clients.Value
        return fun msg ->
            dprintfn "Received message %A from %s" msg clientIP
            match msg with
            | Message data -> 
                match data with
                | C2SMessage.Ping ->
                    client.Post S2CMessage.Pong
            | Error exn -> 
                dprintfn "Error in WebSocket server connected to %s: %s" clientIP exn.Message
                //client.Post (S2CMessage.Fatal ("Error: " + exn.Message))
            | Close ->
                dprintfn "Closed connection to %s" clientIP
    }
