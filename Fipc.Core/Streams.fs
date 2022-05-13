namespace Fipc.Core

open System.IO
open Fipc.Core.Common

/// Streams allow clients to stream data out from a server. This is read only (generally).
module Streams =
    
    let clientHandler (configuration: FipcConnectionConfiguration) (outputChannel: FipcConnectionWriter) (stream: FipcStream) =
        let s = stream.GetStream()
        
        let rec loop() =
            Operations.tryReadMessage configuration.ContentType s
            |> Result.bind outputChannel.TryPostMessage
            |> ignore
            // Todo handle this.
            loop ()
                   
        match Handshake.clientHandler FipcConnectionType.Stream configuration s with
        | Ok _ ->
            loop ()
        | Error e -> failwith $"Handshake error: {e}"
        
    let serverHandler (configuration: FipcConnectionConfiguration) (inputChannel: FipcConnectionReader) (stream: FipcStream) =
        let s = stream.GetStream()
        
        let rec loop () =
            // Try read the header.
            //match Operations.tr
            match inputChannel.TryReadMessage() with
            | Some msg ->
                match Operations.tryWriteMessage s msg with
                | Ok _ -> ()
                | Error e ->
                    // Log error.
                    ()
            | None ->
                // No message to read attempt. Sleep for a bit to save some cylces.
                Async.Sleep 100 |> Async.RunSynchronously
            loop ()
        
        match Handshake.serverHandler FipcConnectionType.Stream configuration s with
        | Ok _ ->
            loop ()
        | Error e -> failwith $"Handshake error: {e}"