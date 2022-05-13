// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Net
open System.Text
open Fipc.Core
open Fipc.Core.Common
open Fipc.Messaging.Infrastructure
open Freql.Sqlite

/// An example listener, this will simply print the messages it receives.
let listener (reader: FipcConnectionReader) =
    let rec testLoop () =
        match reader.TryReadMessage() with
        | Some msg ->
            match msg.IsHeartBeat(), msg.Body with
            | false, FipcMessageContent.Text t -> printfn $"Message: {t}"
            //| FipcMessageContent.Empty
            | true, _ -> printfn $"Heart beat received"
            | _ -> printfn $"Message type not supported yet."
        | None -> () //printfn $"No messages."

        Async.Sleep 1000 |> Async.RunSynchronously
        testLoop ()

    printfn $"Starting example listener loop."
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

    let streamServer _ =
        let config =
            ({ Id = "example-client"
               ChannelType = FipcChannelType.NamedPipe "testpipe"
               MaxThreads = 1
               ContentType = FipcContentType.Text
               EncryptionType = FipcEncryptionType.None
               CompressionType = FipcCompressionType.None
               Key = Encoding.UTF8.GetBytes "Hello, World!" }: FipcConnectionConfiguration)

        let writer = Server.startStreamServer config

        let rec testLoop () =
            printfn "Enter message to send to server"
            printf "> "
            let message = Console.ReadLine()

            match writer.TryPostMessage(FipcMessage.StringMessage($"Time: {DateTime.Now} Message: {message}")) with
            | Ok _ -> ()
            | Error e -> printfn $"{e}"

            testLoop ()

        printfn "Starting server loop."
        testLoop ()

    let messageBus _ =
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

        // 3. Connect to database.
        let ctx = SqliteContext.Open("C:\\ProjectData\\Fipc\\message_bus.db")

        // 4. Run the message bus.
        let messageBus =
            ({ Readers = [ "test_reader", reader ] |> Map.ofList
               Writers = Map.empty
               Handler = fun wm -> Ok [ MessageBusOperation.Store ]
               OperationsResultHandler =
                   fun mr ->
                       printfn $"Result for message {mr.Message.Message.Header.Correlation.ToBase64()}:"
                       mr.OperationResults
                       |> List.map (fun r -> printfn $"\t{r}")
                       |> ignore
               Delay = 100 }: MessageBus)
            
        messageBus.Run(ctx)

    let tcpHookServer _ =

        // 1. An example configuration.
        let config =
            ({ Id = "example-server"
               ChannelType = FipcChannelType.Tcp { Address = IPAddress.Parse("127.0.0.1"); Port = 12345 }
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

    // Examples.hookServer ()
    //Examples.streamServer ()
    //Examples.messageBus ()
    Examples.tcpHookServer ()
    
    0 // return an integer exit code
