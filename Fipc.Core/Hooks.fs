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
        // Get a task representing the completion of the channel (i.e. no more messages).
        // When complete it will close the client and end the stream.
        let complete = inputChannel.GetCompletion()
        
        let rec loop (i: int) =
            let cont i =
                match complete.IsCompleted || complete.IsCanceled with
                | true -> ()
                | false -> loop(i)
     
            match inputChannel.TryReadMessage() with
            | Some msg ->
                match Operations.tryWriteMessage s msg with
                | Ok _ -> cont 0
                | Error e ->
                    // Log error.
                    cont (i + 1)
            | None ->
                    // No message to read attempt. Sleep for a bit to save some cylecs.
                    Async.Sleep 100 |> Async.RunSynchronously
                    // Send heartbeat
                    match i >= 50 with
                    | true ->
                        cont (0)
                        //match Operations.tryWriteMessage s (FipcMessage.HeartBeat()) with
                        //| Ok _ -> loop (0)
                        //| Error e -> loop (i + 1)
                    | false -> cont (i + 1)

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
        
