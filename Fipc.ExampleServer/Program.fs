// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System.Text
open Fipc.Core
open Fipc.Core.Common

[<EntryPoint>]
let main argv =

    let config = ({
        Id = "example-server"
        ChannelType = FipcChannelType.NamedPipe "testpipe"
        MaxThreads = 1
        ContentType = FipcContentType.Text
        EncryptionType = FipcEncryptionType.None
        CompressionType = FipcCompressionType.None
        Key = Encoding.UTF8.GetBytes "Hello, World!"
    }: FipcConnectionConfiguration)
    
    
    let reader = Server.startHookServer config 
    
    let rec testLoop () =
        match reader.TryReadMessage() with
        | Some msg ->
            match msg.Body with
            | FipcMessageContent.Text t -> printfn $"Message: {t}"
            | _ -> printfn $"Message type not supported yet."
        | None -> printfn $"No messages."
        Async.Sleep 1000 |> Async.RunSynchronously
        testLoop ()
    
    printfn $"Starting server loop."
    testLoop ()    
    0 // return an integer exit code