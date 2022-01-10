// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Text
open Fipc.Core
open Fipc.Core.Common


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





[<EntryPoint>]
let main argv =

    
    Examples.hookClient ()
    0 // return an integer exit code
