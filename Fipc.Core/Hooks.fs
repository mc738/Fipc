namespace Fipc.Core

open System.IO
open System.Threading.Channels
open Fipc.Core.Common

/// Hooks allow clients to send data to a server. This is write only (generally).
/// Within a client application a hook can be called to do something on the server.
/// Much like http hooks.    
module Hooks =

    let clientHandler (inputChannel: FipcConnectionReader) (id: string) (stream: Stream) =
        let rec loop () =
            // Try read the header.
            //match Operations.tr
            match inputChannel.TryReadMessage() with
            | Some msg ->
                match Operations.tryWriteMessage id stream msg with
                | Ok _ -> ()
                | Error e ->
                    // Log error.
                    ()
            | None ->
                // No message to read attempt. Sleep for a bit to save some cylces.
                Async.Sleep 100 |> Async.RunSynchronously
            loop ()
        loop ()
        
    let serverHandler (contentType: FipcContentType) (outputChannel: FipcConnectionWriter) (id: string)  (stream: Stream) =
        let rec loop() =
            Operations.tryReadMessage id contentType stream
            |> Result.bind outputChannel.TryPostMessage
            |> ignore
            // Todo handle this.
            loop ()
            
        loop ()
