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
                async { NamedPipes.Server.startHookServer configuration name (connector.GetWriter()) }

            Async.Start background
        | FipcChannelType.Tcp connection ->
            let background = Tcp.Server.startHookServer configuration connection (connector.GetWriter())
            
            Async.Start background
            
        connector.GetReader()
        
    let startStreamServer (configuration: FipcConnectionConfiguration) =
        let connector = FipcConnector.Create()

        match configuration.ChannelType with
        | FipcChannelType.NamedPipe name ->
            let background =
                async { NamedPipes.Server.startStreamServer configuration name (connector.GetReader()) }

            Async.Start background
        | FipcChannelType.Tcp connection ->
            let background = Tcp.Server.startStreamServer configuration connection (connector.GetReader())
            
            Async.Start background

        connector.GetWriter()
