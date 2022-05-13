namespace Fipc.Core

open System.IO
open System.Threading.Channels
open Fipc.Core.Common

/// Hooks allow clients to send data to a server. This is write only (generally).
/// Within a client application a hook can be called to do something on the server.
/// Much like http hooks.    
module Hooks =

    let clientHandler (configuration: FipcConnectionConfiguration) (inputChannel: FipcConnectionReader) (stream: FipcStream) =
        let s = stream.GetStream()
        
        let rec loop (i: int) =
            // Try read the header.
            //match Operations.tr
            match inputChannel.TryReadMessage() with
            | Some msg ->
                match Operations.tryWriteMessage s msg with
                | Ok _ -> loop (0)
                | Error e ->
                    // Log error.
                    loop (i + 1)
            | None ->
                // No message to read attempt. Sleep for a bit to save some cylecs.
                Async.Sleep 100 |> Async.RunSynchronously
                // Send heartbeat
                match i >= 50 with
                | true -> loop (0)
                    //match Operations.tryWriteMessage s (FipcMessage.HeartBeat()) with
                    //| Ok _ -> loop (0)
                    //| Error e -> loop (i + 1)
                | false -> loop (i + 1)

        match Handshake.clientHandler FipcConnectionType.Hook configuration s with
        | Ok _ ->
            loop (0)
        | Error e -> failwith $"Handshake error: {e}"
        
    let serverHandler (configuration: FipcConnectionConfiguration) (outputChannel: FipcConnectionWriter) (stream: FipcStream) =
        let s = stream.GetStream()
        //s.ReadTimeout <- 10000
        
        let rec loop() =
            try
                //printfn "Waiting for message..."
                Operations.tryReadMessage configuration.ContentType s
                |> Result.bind outputChannel.TryPostMessage
                |> ignore
                // Todo handle this.
                //printfn "Looping"
                //match configuration.ChannelType with
                //| FipcChannelType.NamedPipe _ ->
                //    stream :?>
            with
            | exn -> printfn $"Exn: {exn.Message}"
            
            
            //stream.
            if stream.IsConnected() then loop ()
            else printfn "Closing connection"
                        
        match Handshake.serverHandler FipcConnectionType.Hook configuration s with
        | Ok _ ->
            loop ()
        | Error e -> failwith $"Handshake error: {e}"
        
