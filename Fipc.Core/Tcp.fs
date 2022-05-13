namespace Fipc.Core

open System.Net.Sockets
open Fipc.Core.Common

module Tcp =
    
    module Server =
        
        
        let start configuration (connection: TcpConnection) (handlerFn: FipcStream -> unit) =
            let listener = TcpListener(connection.Address, connection.Port)
            listener.Start()
            printfn $"Listening on {connection.Address}:{connection.Port}"
            
            let rec loop() = async {
                printfn "Waiting for connection..."
                let! client = listener.AcceptTcpClientAsync() |> Async.AwaitTask
                printfn "New connection"
                
                async { handlerFn <| FipcStream.Network (client.GetStream()) } |> Async.Start
                
                return! loop()
            }
            
            loop()
            
        let startHookServer configuration (connection: TcpConnection) outputWriter =
            start configuration connection (Hooks.serverHandler configuration outputWriter)
            
        let startStreamServer configuration (connection: TcpConnection) inputReader =
            start configuration connection (Streams.serverHandler configuration inputReader)
        
    
    module Client =
        
        let start (connection: TcpConnection) (handlerFn: FipcStream -> unit) = async {
            use client = new TcpClient()
            printfn $"Connecting to {connection.Address}:{connection.Port}"
            client.Connect(connection.Address, connection.Port)
            handlerFn <| FipcStream.Network (client.GetStream())
            return ()
        }
        
        let startHookClient configuration connection inputReader =
            start connection (Hooks.clientHandler configuration inputReader)
            
        let startStreamClient configuration connection outputWriter =
            start connection (Streams.clientHandler configuration outputWriter)