namespace MyCS01

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "/log">] Log

module Templating =
    open WebSharper.UI.Html

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
            let isActive = if endpoint = act then "nav-link active" else "nav-link"
            li [attr.``class`` "nav-item"] [
                a [
                    attr.``class`` isActive
                    attr.href (ctx.Link act)
                ] [text txt]
            ]
        [
            "Home" => EndPoint.Home
            "About" => EndPoint.About
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Templates.MainTemplate()
            .Title(title)
            .MenuBar(MenuBar ctx action)
            .Body(body)
            .Doc()

module Site =
    open WebSharper.UI.Html

    open type WebSharper.UI.ClientServer

    let HomePage ctx =
        LogWebSocketServer.Log "Home page is requested"
        Content.Page(
            Templating.Main ctx EndPoint.Home "Home" [
                h1 [] [text "Say Hi to the server!"]
                div [] [client (Client.Main())]
            ], 
            Bundle = "home"
        )

    let AboutPage ctx =
        LogWebSocketServer.Log "About page is requested"
        Content.Page(
            Templating.Main ctx EndPoint.About "About" [
                h1 [] [text "About"]
                p [] [text "This is a template WebSharper client-server application."]
            ], 
            Bundle = "about"
        )

    let LogPage (ctx: Context<_>, wsep) =
        LogWebSocketServer.Log "Log page is requested"
        // Serve the log viewer page
        Content.Page [
            div [] [client (LogWebSocketClient.LogViewer wsep)]
        ]

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.About -> AboutPage ctx
            | EndPoint.Log ->
                let wsep = LogWebSocketClient.MyEndPoint (ctx.RequestUri.ToString())
                LogPage(ctx, wsep)
        )

