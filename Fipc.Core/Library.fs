namespace Fipc.Core

open System
open System.IO
open System.IO.Pipes
open System.Reflection.Emit
open System.Security.Principal
open System.Text
open System.Threading

module Say =
    let hello name = printfn "Hello %s" name

[<AutoOpen>]
module Common =

    let magicBytes = [| 14uy; 6uy |]
    
    let readString size (stream: Stream) =
        let buffer = Array.zeroCreate size
        stream.Read(buffer, 0, size) |> ignore
        Encoding.UTF8.GetString buffer


    let write (data: byte array) (stream: Stream) =
        stream.Write(data, 0, data.Length)
        stream.Flush()

    let writeString (value: string) (stream: Stream) =
        stream.Write(value |> Encoding.UTF8.GetBytes, 0, value.Length)
        stream.Flush()


    // Message layout.
    // === Header ===
    // 0 - Magic byte
    // 1 - Magic byte
    // 2 - len 1
    // 3 - len 2
    // 4 - type
    // 5 - opcode
    // 6 - encryption
    // 7 - compression
    // === Body ===
    // 8 onwards - Data (?)

    [<RequireQualifiedAccess>]
    type FipcOpCode =
        | Send
        | Receive
        | Close
        | Unknown of byte

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcOpCode.Send
            | 2uy -> FipcOpCode.Receive
            | 0uy -> FipcOpCode.Close
            | _ -> Unknown b

        member op.ToByte() =
            match op with
            | FipcOpCode.Send -> 1uy
            | FipcOpCode.Receive -> 2uy
            | FipcOpCode.Close -> 0uy
            | FipcOpCode.Unknown b -> b

    [<RequireQualifiedAccess>]
    type FipcTypeCode =
        | Text
        | Json
        | Bytes

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcTypeCode.Text
            | 2uy -> FipcTypeCode.Json
            | _ -> FipcTypeCode.Bytes

        member t.ToByte() =
            match t with
            | FipcTypeCode.Text -> 1uy
            | FipcTypeCode.Json -> 2uy
            | FipcTypeCode.Bytes -> 0uy


    [<RequireQualifiedAccess>]
    type FipcEncryptionCode =
        | None
        | Aes

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcEncryptionCode.Aes
            | _ -> FipcEncryptionCode.None

        member ec.ToByte() =
            match ec with
            | FipcEncryptionCode.None -> 0uy
            | FipcEncryptionCode.Aes -> 1uy


    [<RequireQualifiedAccess>]
    type FipcCompressionCode =
        | None
        | GZip

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcCompressionCode.GZip
            | _ -> FipcCompressionCode.None

        member ec.ToByte() =
            match ec with
            | FipcCompressionCode.None -> 0uy
            | FipcCompressionCode.GZip -> 1uy



    type MessageHeader =
        { Length: int
          Type: FipcTypeCode
          Op: FipcOpCode
          Encryption: FipcEncryptionCode
          Compression: FipcCompressionCode }

        static member HeaderLength = 8

        static member Create
            (
                length: int,
                typeCode: FipcTypeCode,
                opCode: FipcOpCode,
                encryptionCode: FipcEncryptionCode,
                compressionCode: FipcCompressionCode
            ) =
            { Length = length
              Type = typeCode
              Op = opCode
              Encryption = encryptionCode
              Compression = compressionCode }

        static member TryParse(buffer: byte array) =
            match buffer.Length >= 8 with
            | true ->                
                let len = BitConverter.ToInt32(buffer, 0)
                printfn $"Len: {len}"

                { Length = len
                  Type = FipcTypeCode.FromByte buffer.[4]
                  Op = FipcOpCode.FromByte buffer.[5]
                  Encryption = FipcEncryptionCode.FromByte buffer.[6]
                  Compression = FipcCompressionCode.FromByte buffer.[7] }
                |> Ok
            | false -> Error $"Buffer is too short (length: {buffer.Length})"

        member mh.Serialize() =
            Array.concat [ magicBytes
                           BitConverter.GetBytes mh.Length
                           [| mh.Type.ToByte()
                              mh.Op.ToByte()
                              mh.Encryption.ToByte()
                              mh.Compression.ToByte() |] ]

    let readMessageHeader (stream: Stream) =
        let buffer = Array.zeroCreate 10
        stream.Read(buffer, 0, 8) |> ignore
        MessageHeader.TryParse buffer
        
        
    let waitForMessage (id: string) (stream: Stream) =
        printfn $"[{id}] Waiting for message."
        let buffer = Array.zeroCreate 2
        stream.Read(buffer, 0, 2) |> ignore
        match buffer = magicBytes with
        | true ->
            printfn "Message received."
            match readMessageHeader stream with
            | Ok mh ->
                printfn $"[{id}] Message header received: {mh}"
                let msg = readString mh.Length stream
                printfn $"[{id}] Message: {msg}"
                Ok msg
            | Error e ->
                printfn $"[{id}] ERROR: {e}"
                Error e
        | false ->
            Error "Magic bytes do not match"
        
module Server =

    open Common

    let numThread = 4

    let serverThread (name: string) (direction: PipeDirection) (maxThreads: int) (data: obj) =
        use pipeServer =
            new NamedPipeServerStream(name, direction, maxThreads)

        let id = Thread.CurrentThread.ManagedThreadId

        printfn $"[{id}] Waiting for connection..."
        pipeServer.WaitForConnection()
        printfn $"[{id}] Connection."

        try
            let message = "Hello, client."
            let messageBody = Encoding.UTF8.GetBytes message

            let header =
                MessageHeader.Create(
                    messageBody.Length,
                    FipcTypeCode.Text,
                    FipcOpCode.Receive,
                    FipcEncryptionCode.None,
                    FipcCompressionCode.None
                )

            // Write the header.
            //write  pipeServer
            // Possible - read ack
            // Write body.
            let h = (header.Serialize())
            write (Array.concat [ h; messageBody ]) pipeServer

            let rec loop () =
                printfn $"***"
                match waitForMessage (id.ToString()) pipeServer with
                | Ok msg -> loop ()
                | Error e -> ()
 
            loop ()
        with
        | ex -> printfn $"[{id}] ERROR: {ex.Message}"

        pipeServer.Close()

    let start name maxThreads =
        let servers =
            [ 0 .. (numThread - 1) ]
            |> List.map
                (fun i ->
                    printfn $"Starting thread {i}"

                    let thread =
                        Thread(ParameterizedThreadStart(serverThread name PipeDirection.InOut maxThreads))

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

module Client =

    let connection name =
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

        match waitForMessage "client" pipeClient with
        | Ok msg ->
            //printfn $"Message: {msg}"
            let rec writeLoop () =
                printfn $"Connected: {pipeClient.IsConnected}"
                let message = $"Time now is: {DateTime.Now}"
                let messageBody = Encoding.UTF8.GetBytes message

                let header =
                    MessageHeader.Create(
                        messageBody.Length,
                        FipcTypeCode.Text,
                        FipcOpCode.Receive,
                        FipcEncryptionCode.None,
                        FipcCompressionCode.None
                    )

                // Write the header.
                //write (header.Serialize()) pipeClient
                // Possible - read ack
                // Write body.
                write (Array.concat [ header.Serialize(); messageBody ]) pipeClient

                Async.Sleep 5000 |> Async.RunSynchronously
                writeLoop ()
            writeLoop ()
        | Error e -> printfn $"ERROR: {e}"
