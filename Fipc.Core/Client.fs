namespace Fipc.Core
open Fipc.Core.Common

module Client =
    
    let startHookClient (configuration: FipcConnectionConfiguration) =
        let connector = FipcConnector.Create()

        match configuration.ChannelType with
        | FipcChannelType.NamedPipe name ->
            let background =
                async {
                    NamedPipes.Client.startHookClient configuration name (connector.GetReader()) 
                }

            Async.Start background
        | FipcChannelType.Tcp connection ->
            Tcp.Client.startHookClient configuration connection (connector.GetReader()) |> Async.Start
            
        connector.GetWriter()
        
    
    let startStreamClient (configuration: FipcConnectionConfiguration) =
        let connector = FipcConnector.Create()

        match configuration.ChannelType with
        | FipcChannelType.NamedPipe name ->
            let background =
                async {
                    NamedPipes.Client.startStreamClient configuration name (connector.GetWriter()) 
                }

            Async.Start background
        | FipcChannelType.Tcp connection ->
            Tcp.Client.startStreamClient configuration connection (connector.GetWriter()) |> Async.Start
            
        connector.GetReader()