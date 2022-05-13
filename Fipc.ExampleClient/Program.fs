// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Net
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

    let hookClient _ =
        let config =
            ({ Id = "example-client"
               ChannelType = FipcChannelType.NamedPipe "testpipe"
               MaxThreads = 1
               ContentType = FipcContentType.Text
               EncryptionType = FipcEncryptionType.None
               CompressionType = FipcCompressionType.None
               Key = Encoding.UTF8.GetBytes "Hello, World!" }: FipcConnectionConfiguration)

        let writer = Client.startHookClient config

        let rec testLoop () =
            printfn "Enter message to send to server"
            printf "> "
            let message = Console.ReadLine()
            match writer.TryPostMessage(FipcMessage.StringMessage($"Time: {DateTime.Now} Message: {message}")) with
            | Ok _ -> ()
            | Error e -> printfn $"{e}"
            testLoop ()

        printfn $"Starting client loop."
        testLoop ()

    let streamClient _ =
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
        let reader = Client.startStreamClient config
        
        // 3. Use the reader. In this example a loop is run that listens for messages and prints them.
        listener reader
        
    let tcpHookClient _ =
        let config =
            ({ Id = "example-client"
               ChannelType = FipcChannelType.Tcp { Address = IPAddress.Parse("127.0.0.1"); Port = 12345 }
               MaxThreads = 1
               ContentType = FipcContentType.Text
               EncryptionType = FipcEncryptionType.None
               CompressionType = FipcCompressionType.None
               Key = Encoding.UTF8.GetBytes "Hello, World!" }: FipcConnectionConfiguration)

        let writer = Client.startHookClient config

        let rec testLoop () =
            printfn "Enter message to send to server"
            printf "> "
            let message = Console.ReadLine()
            match writer.TryPostMessage(FipcMessage.StringMessage($"Time: {DateTime.Now} Message: {message}")) with
            | Ok _ -> ()
            | Error e -> printfn $"{e}"
            testLoop ()

        printfn $"Starting tcp client loop."
        testLoop ()
        



[<EntryPoint>]
let main argv =

    
    //Examples.hookClient ()
    // Examples.streamClient ()
    Examples.tcpHookClient ()
    0 // return an integer exit code
