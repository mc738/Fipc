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
            (handlerFn: Stream -> unit)
            (data: obj)
            =
            use pipeServer =
                new NamedPipeServerStream(name, direction, maxThreads)

            let id = Thread.CurrentThread.ManagedThreadId

            printfn $"[{id}] Waiting for connection..."
            pipeServer.WaitForConnection()
            printfn $"[{id}] Connection."

            handlerFn pipeServer

            pipeServer.Close()

        let start configuration name (handlerFn: Stream -> unit) =
            let servers =
                [ 0 .. (configuration.MaxThreads - 1) ]
                |> List.map
                    (fun i ->
                        printfn $"Starting thread {i}"

                        let thread =
                            Thread(
                                ParameterizedThreadStart(
                                    serverThread name PipeDirection.InOut configuration.MaxThreads handlerFn
                                )
                            )

                        thread.Start()
                        thread)

            let rec loop (servers: Thread list) =
                match servers.IsEmpty with
                | true -> ()
                | false ->
                    servers
                    |> List.map
                        (fun s ->
                            match s.Join(250) with
                            | true ->
                                printfn $"Server thread [{s.ManagedThreadId}] has finished. Closing thread."
                                None
                            | false -> Some s)
                    |> List.choose id
                    |> fun s -> loop (s)

            loop (servers)

        let startHookServer configuration (name: string) outputWriter =
            start configuration name (Hooks.serverHandler configuration outputWriter)

    module Client =

        let connection name (handlerFn: Stream -> unit) =
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

            handlerFn pipeClient

        let startHookClient configuration (name: string) inputReader =
            connection name (Hooks.clientHandler configuration inputReader) 