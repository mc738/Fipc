namespace Fipc.Core

open System.IO
open Fipc.Core.Common

[<RequireQualifiedAccess>]
module Handshake =

    let clientHandler
        (connectionType: FipcConnectionType)
        (configuration: FipcConnectionConfiguration)
        (stream: Stream)
        =
        // Send config type + key (?)

        let serializedConfig =
            [| connectionType.ToByte()
               configuration.ContentType.ToByte()
               configuration.EncryptionType.ToByte()
               configuration.CompressionType.ToByte() |]

        Array.concat [ serializedConfig; configuration.Key ]
        |> FipcMessage.BytesMessage
        |> Operations.tryWriteMessage stream
        |> Result.bind
            (fun _ ->
                printfn "Sent "
                // If the first message wrote ok, try read the response.
                Operations.tryReadMessage FipcContentType.Bytes stream
                |> Result.bind
                    (fun m ->
                        match m.Body.Serialize().[0] with
                        | 0uy -> Ok()
                        | 1uy -> Error "Wrong connection type"
                        | 2uy -> Error "Wrong content type"
                        | 3uy -> Error "Wrong encryption type"
                        | 4uy -> Error "Wrong compression type"
                        | 5uy -> Error "Wrong key"
                        | _ -> Error "Unknown response code."))


    let serverHandler
        (connectionType: FipcConnectionType)
        (configuration: FipcConnectionConfiguration)
        (stream: Stream)
        =
        Operations.tryReadMessage FipcContentType.Bytes stream
        |> Result.bind
            (fun m ->

                let body = m.Body.Serialize()

                let requestedConnectionType = FipcConnectionType.FromByte body.[0]
                let contentType = FipcContentType.FromByte body.[1]
                let encryptionType = FipcEncryptionType.FromByte body.[2]
                let compressionType = FipcCompressionType.FromByte body.[3]
                let receivedKey = body.[4..]

                match requestedConnectionType = connectionType,
                      contentType = configuration.ContentType,
                      encryptionType = configuration.EncryptionType,
                      compressionType = configuration.CompressionType,
                      receivedKey = configuration.Key with
                | true, true, true, true, true ->
                    FipcMessage.BytesMessage [| 0uy |]
                    |> Operations.tryWriteMessage stream
                | false, _, _, _, _ ->
                    FipcMessage.BytesMessage [| 1uy |]
                    |> Operations.tryWriteMessage stream
                    |> ignore

                    Error $"Wrong connection type. Requested: {requestedConnectionType} Actual: {connectionType}"
                | _, false, _, _, _ ->
                    FipcMessage.BytesMessage [| 2uy |]
                    |> Operations.tryWriteMessage stream
                    |> ignore

                    Error $"Wrong content type. Requested: {connectionType} Actual: {configuration.ContentType}"
                | _, _, false, _, _ ->
                    FipcMessage.BytesMessage [| 2uy |]
                    |> Operations.tryWriteMessage stream
                    |> ignore

                    Error $"Wrong encryption type. Requested: {encryptionType} Actual: {configuration.EncryptionType}"
                | _, _, _, false, _ ->
                    FipcMessage.BytesMessage [| 3uy |]
                    |> Operations.tryWriteMessage stream
                    |> ignore

                    Error $"Wrong encryption type. Requested: {encryptionType} Actual: {configuration.EncryptionType}"
                | _, _, _, _, false ->
                    FipcMessage.BytesMessage [| 4uy |]
                    |> Operations.tryWriteMessage stream
                    |> ignore

                    Error $"Wrong key. Received: {receivedKey}")