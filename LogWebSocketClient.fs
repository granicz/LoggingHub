module LogWebSocketClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Client

module Server = LogWebSocketServer

[<JavaScript>]
let LogViewer (endpoint: WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage>) =
    let container = Elt.pre [] []
    let writen fmt =
        Printf.ksprintf (fun s ->
            JS.Document.CreateTextNode(s + "\n")
            |> container.Dom.AppendChild
            |> ignore
        ) fmt
    async {
        let! server =
            Client.Connect endpoint <| fun agent -> async {
                return fun msg ->
                    match msg with
                    | Message data ->
                        match data with
                        | Server.S2CMessage.LogMessage(i, msg) ->
                            writen "[%d] %s" i msg
                        | Server.S2CMessage.Fatal msg ->
                            writen "ERROR: %s" msg
                        | Server.S2CMessage.Pong ->
                            ()
                    | Close ->
                        writen "WebSocket connection closed."
                    | Open ->
                        writen "WebSocket connection open."
                    | Error ->
                        writen "WebSocket connection error!"
            }
        return ()
    }
    |> Async.Start

    container

let MyEndPoint (url: string) : WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage> = 
    WebSocketEndpoint.Create(url, "/logws")
