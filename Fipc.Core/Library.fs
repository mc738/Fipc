namespace Fipc.Core.Library

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

    let read size (stream: Stream) =
        let buffer = Array.zeroCreate size
        stream.Read(buffer, 0, size) |> ignore
        buffer

    let readString size (stream: Stream) =
        read size stream |> Encoding.UTF8.GetString

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

    [<RequireQualifiedAccess>]
    type FipcContentType =
        | Text
        | Binary

    [<RequireQualifiedAccess>]
    type MessageContent =
        | Text of string
        | Binary of byte array

    type Message =
        { Header: MessageHeader
          Body: MessageContent }

    type SendAction = unit -> Message

    type ReceiveAction = Message -> unit

    //type

    let readMessageHeader (stream: Stream) =
        let buffer = Array.zeroCreate 10
        stream.Read(buffer, 0, 8) |> ignore
        MessageHeader.TryParse buffer

    let waitForMessage (id: string) (contentType: FipcContentType) (stream: Stream) =
        printfn $"[{id}] Waiting for message."
        let buffer = Array.zeroCreate 2
        stream.Read(buffer, 0, 2) |> ignore

        match buffer = magicBytes with
        | true ->
            printfn "Message received."

            match readMessageHeader stream with
            | Ok mh ->
                printfn $"[{id}] Message header received: {mh}"

                match contentType with
                | FipcContentType.Text ->
                    let msg = readString mh.Length stream
                    printfn $"[{id}] Message: {msg}"

                    Ok
                        { Header = mh
                          Body = MessageContent.Text msg }
                | FipcContentType.Binary ->
                    let msg = read mh.Length stream

                    Ok
                        { Header = mh
                          Body = MessageContent.Binary msg }
            | Error e ->
                printfn $"[{id}] ERROR: {e}"
                Error e
        | false -> Error "Magic bytes do not match"

    let handleMessage (message: Message) (sendAction: SendAction) (receiveAction: ReceiveAction) (stream: Stream) =
        match message.Header.Op with
        | FipcOpCode.Receive ->
            receiveAction message

            () // Read.
        | FipcOpCode.Send -> () // Write.
        | FipcOpCode.Close -> () // close the connection.
        | FipcOpCode.Unknown _ -> () // error?

    [<RequireQualifiedAccess>]
    type ServerType =
        | TextReadOnly
        | TextWriteOnly
        | TextReadWrite
        | JsonReadOnly
        | JsonWriteOnly
        | JsonReadWrite
        | BinaryReadOnly
        | BinaryWriteOnly
        | BinaryReadWrite
        | AnyReadOnly
        | AnyWriteOnly
        | Any

        static member FromByte(b: byte) =
            match b with
            | 1uy -> ServerType.TextReadOnly
            | 2uy -> ServerType.TextWriteOnly
            | 3uy -> ServerType.TextReadWrite
            | 4uy -> ServerType.JsonReadOnly
            | 5uy -> ServerType.JsonWriteOnly
            | 6uy -> ServerType.JsonReadWrite
            | 7uy -> ServerType.BinaryReadOnly
            | 8uy -> ServerType.BinaryWriteOnly
            | 9uy -> ServerType.BinaryReadWrite
            | 10uy -> ServerType.AnyReadOnly
            | 11uy -> ServerType.AnyWriteOnly
            | _ -> ServerType.Any

        member st.ToByte() =
            match st with
            | ServerType.TextReadOnly -> 1uy
            | ServerType.TextWriteOnly -> 2uy
            | ServerType.TextReadWrite -> 3uy
            | ServerType.JsonReadOnly -> 4uy
            | ServerType.JsonWriteOnly -> 5uy
            | ServerType.JsonReadWrite -> 6uy
            | ServerType.BinaryReadOnly -> 7uy
            | ServerType.BinaryWriteOnly -> 8uy
            | ServerType.BinaryReadWrite -> 9uy
            | ServerType.AnyReadOnly -> 10uy
            | ServerType.AnyWriteOnly -> 11uy
            | ServerType.Any -> 0uy

module Server =

    open Common


    let numThread = 4

    let handshake (serverType: ServerType) (stream: Stream) =
        try
            let (tc, oc) =
                match serverType with
                | ServerType.TextReadOnly -> (FipcTypeCode.Text, FipcOpCode.Receive)
                | ServerType.TextWriteOnly -> (FipcTypeCode.Text, FipcOpCode.Send)
                | ServerType.TextReadWrite -> (FipcTypeCode.Text, FipcOpCode.Receive)
                | ServerType.JsonReadOnly -> (FipcTypeCode.Json, FipcOpCode.Receive)
                | ServerType.JsonWriteOnly -> (FipcTypeCode.Json, FipcOpCode.Send)
                | ServerType.JsonReadWrite -> (FipcTypeCode.Json, FipcOpCode.Receive)
                | ServerType.BinaryReadOnly -> (FipcTypeCode.Bytes, FipcOpCode.Receive)
                | ServerType.BinaryWriteOnly -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                | ServerType.BinaryReadWrite -> (FipcTypeCode.Bytes, FipcOpCode.Receive)
                | ServerType.AnyReadOnly -> (FipcTypeCode.Bytes, FipcOpCode.Receive)
                | ServerType.AnyWriteOnly -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                | ServerType.Any -> (FipcTypeCode.Bytes, FipcOpCode.Receive)

            let header =
                MessageHeader.Create(1, tc, oc, FipcEncryptionCode.None, FipcCompressionCode.None)

            // Write the header.
            //write  pipeServer
            // Possible - read ack
            // Write body.
            let h = (header.Serialize())

            write
                (Array.concat [ h
                                [| serverType.ToByte() |] ])
                stream

            // Read response. If ok. then continue.
            match waitForMessage (id.ToString()) FipcContentType.Binary stream with
            | Ok msg when msg.Body = MessageContent.Binary [| 1uy |] -> Ok()
            | Ok msg -> Error $"[{id}] ERROR: Handshake could not be completed or was connection is incompatible."
            | Error e -> Error $"[{id}] ERROR: {e}"
        with
        | ex -> Error $"[{id}] ERROR: {ex.Message}"

    let serverThread (name: string) (direction: PipeDirection) (maxThreads: int) (serverType: ServerType) (data: obj) =
        use pipeServer =
            new NamedPipeServerStream(name, direction, maxThreads)

        let id = Thread.CurrentThread.ManagedThreadId

        printfn $"[{id}] Waiting for connection..."
        pipeServer.WaitForConnection()
        printfn $"[{id}] Connection."

        try
            match handshake serverType pipeServer with
            | Ok _ -> ()
            | Error e -> printfn $"{e}"

            (*
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
            *)
            // Write the header.
            //write  pipeServer
            // Possible - read ack
            // Write body.
            (*
            let h = (header.Serialize())
            write (Array.concat [ h; messageBody ]) pipeServer
            *)

            let rec loop () =
                printfn $"***"

                match waitForMessage (id.ToString()) FipcContentType.Text pipeServer with
                | Ok msg -> loop ()
                | Error e -> ()

            loop ()
        with
        | ex -> printfn $"[{id}] ERROR: {ex.Message}"

        pipeServer.Close()

    let start name maxThreads (serverType: ServerType) =
        let servers =
            [ 0 .. (numThread - 1) ]
            |> List.map
                (fun i ->
                    printfn $"Starting thread {i}"

                    let thread =
                        Thread(ParameterizedThreadStart(serverThread name PipeDirection.InOut maxThreads serverType))

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


// JsonReader


module Client =

    let handshake (serverType: ServerType) (stream: Stream) =
        match waitForMessage "client" FipcContentType.Binary stream with
        | Ok msg ->
            let (tc, oc) =
                match serverType with
                | ServerType.TextReadOnly -> (FipcTypeCode.Text, FipcOpCode.Send)
                | ServerType.TextWriteOnly -> (FipcTypeCode.Text, FipcOpCode.Receive)
                | ServerType.TextReadWrite -> (FipcTypeCode.Text, FipcOpCode.Send)
                | ServerType.JsonReadOnly -> (FipcTypeCode.Json, FipcOpCode.Send)
                | ServerType.JsonWriteOnly -> (FipcTypeCode.Json, FipcOpCode.Receive)
                | ServerType.JsonReadWrite -> (FipcTypeCode.Json, FipcOpCode.Send)
                | ServerType.BinaryReadOnly -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                | ServerType.BinaryWriteOnly -> (FipcTypeCode.Bytes, FipcOpCode.Receive)
                | ServerType.BinaryReadWrite -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                | ServerType.AnyReadOnly -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                | ServerType.AnyWriteOnly -> (FipcTypeCode.Bytes, FipcOpCode.Receive)
                | ServerType.Any -> (FipcTypeCode.Bytes, FipcOpCode.Send)
                        
            match msg.Header.Op, msg.Header.Type with
            | op, st when op = oc && st = tc ->
                let header =
                        MessageHeader.Create(
                            1,
                            tc,
                            oc,
                            FipcEncryptionCode.None,
                            FipcCompressionCode.None
                        )
                write
                    (Array.concat [ header.Serialize()
                                    [| 1uy |] ])
                    stream

                Ok()
                //Ok true
            | op, st -> Error $"Server type and/or op code do not match. Request: {tc} (op: {oc}). Got: {st} (op: {op})"
            | _ -> Error "Server handshake invalid."
            
            (*
            /// Check the types are the same
            match msg.Body with
            | MessageContent.Binary b when b.Length > 0 ->
                match ServerType.FromByte b.[0] = serverType with
                | true ->
                    let header =
                        MessageHeader.Create(
                            1,
                            tc,
                            oc,
                            FipcEncryptionCode.None,
                            FipcCompressionCode.None
                        )
                    write
                        (Array.concat [ header.Serialize()
                                        [| 1uy |] ])
                        stream

                    Ok()
                | false -> Error "Expected and actual server types do not match."
            *)
        | Error e -> Error $"ERROR: {e}"

    let connection name (serverType: ServerType) =
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

        match handshake serverType pipeClient with
        | Ok _ ->
            printfn $"Connected!"
            //
            ()
        | Error e ->
            printfn $"Error: {e}"
        
        (*
        match waitForMessage "client" FipcContentType.Text pipeClient with
        | Ok msg ->

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
                write
                    (Array.concat [ header.Serialize()
                                    messageBody ])
                    pipeClient

                Async.Sleep 5000 |> Async.RunSynchronously
                writeLoop ()

            writeLoop ()
        | Error e -> printfn $"ERROR: {e}"
        *)