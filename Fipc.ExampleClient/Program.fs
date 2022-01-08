// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO.Pipes
open System.Security.Principal
open Fipc.Core
open Fipc.Core.Common

[<EntryPoint>]
let main argv =
    
    let config = ({
        Id = "example-client"
        ChannelType = FipcChannelType.NamedPipe "testpipe"
        MaxThreads = 1
        ContentType = FipcContentType.Text
    }: FipcConnectionConfiguration)
    
    let writer = Client.startHookClient config
    
    let rec testLoop () =
        match writer.TryPostMessage(FipcMessage.StringMessage($"Time now is {DateTime.Now}.")) with
        | Ok _ -> ()
        | Error e -> printfn $"{e}"
        Async.Sleep 1000 |> Async.RunSynchronously
        testLoop ()
        
    printfn $"Starting client loop."
    testLoop ()
    0 // return an integer exit code
