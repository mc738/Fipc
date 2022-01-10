// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System.Text
open Fipc.Core
open Fipc.Core.Common

/// An example listener, this will simply print the messages it receives.
let listener (reader: FipcConnectionReader) =
    let rec testLoop () =
        match reader.TryReadMessage() with
        | Some msg ->
            match msg.Body with
            | FipcMessageContent.Text t -> printfn $"Message: {t}"
            | _ -> printfn $"Message type not supported yet."
        | None -> () //printfn $"No messages."

        Async.Sleep 1000 |> Async.RunSynchronously
        testLoop ()

    printfn $"Starting example server loop."
    testLoop ()

[<RequireQualifiedAccess>]
module Examples =

    /// Example of how to ingrate a hook server.
    let hookServer _ =
        
        // 1. An example configuration.    
        let config =
            ({ Id = "example-server"
               ChannelType = FipcChannelType.NamedPipe "testpipe"
               MaxThreads = 1
               ContentType = FipcContentType.Text
               EncryptionType = FipcEncryptionType.None
               CompressionType = FipcCompressionType.None
               Key = Encoding.UTF8.GetBytes "Hello, World!" }: FipcConnectionConfiguration)
            
        
        // 2. Start the server. This returns a FipcConnectionReader. 
        let reader = Server.startHookServer config
        
        // 3. Use the reader. In this example a loop is run that listens for messages and prints them.
        listener reader
        

[<EntryPoint>]
let main argv =

    Examples.hookServer ()
    0 // return an integer exit code
