namespace Fipc.Core

open System.IO
open System.Threading.Channels
open Fipc.Core.Common

/// Hooks allow clients to send data to a server. This is write only (generally).
/// Within a client application a hook can be called to do something on the server.
/// Much like http hooks.    
module Hooks =

    let clientHandler (configuration: FipcConnectionConfiguration) (inputChannel: FipcConnectionReader) (stream: Stream) =
        let rec loop () =
            // Try read the header.
            //match Operations.tr
            match inputChannel.TryReadMessage() with
            | Some msg ->
                match Operations.tryWriteMessage stream msg with
                | Ok _ -> ()
                | Error e ->
                    // Log error.
                    ()
            | None ->
                // No message to read attempt. Sleep for a bit to save some cylces.
                Async.Sleep 100 |> Async.RunSynchronously
            loop ()
        match Handshake.clientHandler FipcConnectionType.Hook configuration stream with
        | Ok _ ->
            loop ()
        | Error e -> failwith $"Handshake error: {e}"
        
    let serverHandler (configuration: FipcConnectionConfiguration) (outputChannel: FipcConnectionWriter) (stream: Stream) =
        let rec loop() =
            Operations.tryReadMessage configuration.ContentType stream
            |> Result.bind outputChannel.TryPostMessage
            |> ignore
            // Todo handle this.
            loop ()
                        
        match Handshake.serverHandler FipcConnectionType.Hook configuration stream with
        | Ok _ ->
            loop ()
        | Error e -> failwith $"Handshake error: {e}"
        
