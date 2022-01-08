namespace Fipc.Core

open System
open System.IO
open System.IO.Pipes
open System.Security.Principal
open System.Threading

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

        let start name maxThreads (handlerFn: string -> Stream -> unit) =
            let servers =
                [ 0 .. (maxThreads - 1) ]
                |> List.map
                    (fun i ->
                        printfn $"Starting thread {i}"

                        let thread =
                            Thread(
                                ParameterizedThreadStart(
                                    serverThread name PipeDirection.InOut maxThreads (handlerFn (i.ToString()))
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

        let startHookServer (name: string) contentType outputWriter maxThreads =
            start name maxThreads (Hooks.serverHandler contentType outputWriter)

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

        let startHookClient (name: string) inputReader id =
            connection name (Hooks.clientHandler inputReader id) 