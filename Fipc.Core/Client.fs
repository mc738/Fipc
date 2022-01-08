namespace Fipc.Core
open Fipc.Core.Common

module Client =
    
    let startHookClient (configuration: FipcConnectionConfiguration) =
        let connector = FipcConnector.Create()

        match configuration.ChannelType with
        | FipcChannelType.NamedPipe name ->
            let background =
                async {
                    NamedPipes.Client.startHookClient name (connector.GetReader()) configuration.Id
                }

            Async.Start background
        | FipcChannelType.Tcp -> failwith "Not implemented yet."
        connector.GetWriter()
        
        