namespace Fipc.Core

open System
open System.IO
open System.IO.Pipes
open System.Security.Principal
open System.Threading
open Fipc.Core.Common

module NamedPipes =

    module Server =
        //let numThread = 4

        let serverThread
            (name: string)
            (direction: PipeDirection)
            (maxThreads: int)
            (handlerFn: FipcStream -> unit)
            (data: obj)
            =
            use pipeServer =
                new NamedPipeServerStream(name, direction, maxThreads)

            let id = Thread.CurrentThread.ManagedThreadId

            //printfn $"[{id}] Waiting for connection..."
            pipeServer.WaitForConnection()
            //printfn $"[{id}] Connection."

            handlerFn <| FipcStream.PipeServer pipeServer

            printfn $"Complete!"
            pipeServer.Close()

        let start configuration name (handlerFn: FipcStream -> unit) =
            let createServers () =
                [ 0 .. (configuration.MaxThreads - 1) ]
                |> List.map
                    (fun i ->
                        //printfn $"Starting thread {i}"

                        let thread =
                            Thread(
                                ParameterizedThreadStart(
                                    serverThread name PipeDirection.InOut configuration.MaxThreads handlerFn
                                )
                            )

                        thread.Start()
                        thread)

            let rec loop (servers: Thread list) =
                //printfn $"***************** LOOPING"
                match servers.IsEmpty with
                | true ->
                    // Once all threads have been used up start again.
                    loop (createServers ())
                | false ->
                    servers
                    |> List.map
                        (fun s ->
                            match s.Join(250) with
                            | true ->
                                printfn $"Server thread [{s.ManagedThreadId}] has finished. Closing thread."
                                None //<| Thread(s.Name)
                            | false -> Some s)
                    |> List.choose id
                    |> fun s -> loop (s)

            loop (createServers ())

        let startHookServer configuration (name: string) outputWriter =
            start configuration name (Hooks.serverHandler configuration outputWriter)
            
        let startStreamServer configuration (name: string) inputReader =
            start configuration name (Streams.serverHandler configuration inputReader)
    
    module Client =

        let connection name (handlerFn: FipcStream -> unit) =
            let pipeClient =
                new NamedPipeClientStream(
                    ".",
                    name,
                    PipeDirection.InOut,
                    PipeOptions.None,
                    TokenImpersonationLevel.Impersonation
                )

            Console.WriteLine("Connecting to server...\n")
            pipeClient.Connect()

            handlerFn <| FipcStream.PipeClient pipeClient

        let startHookClient configuration (name: string) inputReader =
            connection name (Hooks.clientHandler configuration inputReader)
            
        let startStreamClient configuration (name: string) outputWriter =
            connection name (Streams.clientHandler configuration outputWriter)