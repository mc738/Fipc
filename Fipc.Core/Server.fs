namespace Fipc.Core

open Fipc.Core.Common
open Microsoft.FSharp.Control


[<RequireQualifiedAccess>]
module Server =

    let startHookServer (configuration: FipcConnectionConfiguration) =
        let connector = FipcConnector.Create()

        match configuration.ChannelType with
        | FipcChannelType.NamedPipe name ->
            let background =
                async {
                    NamedPipes.Server.startHookServer
                        name
                        configuration.ContentType
                        (connector.GetWriter())
                        configuration.MaxThreads
                }

            Async.Start background
        | FipcChannelType.Tcp -> failwith "Not implemented yet."

        connector.GetReader()
