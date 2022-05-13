namespace Fipc.Core

open System.IO
open System.Text
open Fipc.Core.Common

[<RequireQualifiedAccess>]    
module Operations =
    
    module Internal =
        
        /// Read a message header from a
        let tryReadMessageHeader (stream: Stream) =
            // Headers are 16 bytes.

            let buffer = Array.zeroCreate 12
            stream.Read(buffer, 0, 12) |> ignore
            FipcMessageHeader.TryParse(buffer, false)

        let tryReadMagicBytes (stream: Stream) =
            match stream.TryRead(4) with
            | Ok buffer ->
                // Check magic bytes.
                match isMagicBytes buffer with
                | true -> Ok ()
                | false -> Error "Magic bytes do not match."
            | Error e -> Error e

    let tryReadMessage (contentType: FipcContentType) (stream: Stream) =
        //printfn $"Waiting for message."

        match Internal.tryReadMagicBytes stream with
        | Ok _ ->
            //printfn $"Message received."

            match Internal.tryReadMessageHeader stream with
            | Ok mh ->
                //printfn $"Message header received: {mh}"

                match stream.TryRead mh.Length, contentType with
                | Ok buffer, FipcContentType.Text ->
                    Ok
                        { Header = mh
                          Body =
                              Encoding.UTF8.GetString buffer
                              |> FipcMessageContent.Text }
                | Ok buffer, FipcContentType.Bytes ->
                    Ok
                        { Header = mh
                          Body = FipcMessageContent.Binary buffer }
                | Ok buffer, FipcContentType.Json ->
                    Ok
                        { Header = mh
                          Body =
                              Encoding.UTF8.GetString buffer
                              |> FipcMessageContent.SerializedJson }
                | Error e, _ -> Error e
            | Error e ->
                Error e
        | Error e -> Error $"Magic bytes do not match. Error: {e}"
        
    let tryWriteMessage (stream: Stream) (message: FipcMessage) =
        match stream.TryWrite(message.Serialize()) with
        | Ok _ -> Ok ()
        | Error e -> Error $"Error: {e}"
