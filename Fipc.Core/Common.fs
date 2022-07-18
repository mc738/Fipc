namespace Fipc.Core

open System
open System.IO
open System.IO.Pipes
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Threading.Channels

[<AutoOpen>]
module Extensions =

    type Stream with

        member stream.TryRead(length: int) =
            match stream.CanRead with
            | true ->
                let buffer = Array.zeroCreate length
                let i = stream.Read(buffer, 0, length) //|> ignore
                //printfn $"*** {i}"
                match i > 0 with
                | true -> Ok buffer
                | false -> Error "No more data"
            | false -> Error "Stream is not readable."

        member stream.TryReadString(length: int) =
            stream.TryRead length
            |> Result.bind (fun d -> Encoding.UTF8.GetString d |> Ok)

        member stream.TryWrite(data: byte array) =
            match stream.CanWrite with
            | true ->
                stream.Write(data, 0, data.Length)
                stream.Flush()
                Ok()
            | false -> Error "Stream is not writeable."

        member stream.TryWriteString(data: string) =
            Encoding.UTF8.GetBytes data |> stream.TryWrite


module Common =

    let magicBytes = [| 14uy; 6uy; 20uy; 9uy |]

    let isMagicBytes (buffer: byte array) = buffer = magicBytes

    let heartBeatCorr = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; |]
    
    // Message layout.
    // Is wrong - update.
    // === Header ===
    // 0 - Magic byte
    // 1 - Magic byte
    // 2 - Magic byte
    // 3 - Magic byte
    // 4 - len 1
    // 5 - len 2
    // 6 - correlation 1
    // 7 - correlation 2
    // -- 4 - type
    // -- 5 - opcode
    // -- 6 - encryption
    // -- 7 - compression
    // === Body ===
    // 8 onwards - Data (?)

    [<RequireQualifiedAccess>]
    type FipcContentType =
        | Text
        | Json
        | Bytes

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcContentType.Text
            | 2uy -> FipcContentType.Json
            | _ -> FipcContentType.Bytes

        member t.ToByte() =
            match t with
            | FipcContentType.Text -> 1uy
            | FipcContentType.Json -> 2uy
            | FipcContentType.Bytes -> 0uy

    [<RequireQualifiedAccess>]
    type FipcEncryptionType =
        | None
        | Aes

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcEncryptionType.Aes
            | _ -> FipcEncryptionType.None

        member ec.ToByte() =
            match ec with
            | FipcEncryptionType.None -> 0uy
            | FipcEncryptionType.Aes -> 1uy

    [<RequireQualifiedAccess>]
    type FipcCompressionType =
        | None
        | GZip

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcCompressionType.GZip
            | _ -> FipcCompressionType.None

        member ec.ToByte() =
            match ec with
            | FipcCompressionType.None -> 0uy
            | FipcCompressionType.GZip -> 1uy

    type FipcCorrelation =
        { Raw: byte array }

        static member HeartBeat() = { Raw = heartBeatCorr }
        
        member fc.ToBase64() = fc.Raw |> Convert.ToBase64String
        
        member fc.IsHeartBeat() = fc.Raw = heartBeatCorr

    [<RequireQualifiedAccess>]
    type FipcConnectionType =
        | Hook
        | Stream
        | Query

        static member FromByte(b: byte) =
            match b with
            | 1uy -> FipcConnectionType.Hook
            | 2uy -> FipcConnectionType.Stream
            | 3uy -> FipcConnectionType.Query
            | _ -> failwith "Unknown connection type."

        member fct.ToByte() =
            match fct with
            | FipcConnectionType.Hook -> 1uy
            | FipcConnectionType.Stream -> 2uy
            | FipcConnectionType.Query -> 3uy

    //[<RequireQualifiedAccess>]
    //type FipcStream =
    //    | NamedPipe of NamedPi

    type TcpConnection = {
        Address: IPAddress
        Port: int
    }
    
    [<RequireQualifiedAccess>]
    type FipcChannelType =
        | NamedPipe of string
        | Tcp of TcpConnection

    [<RequireQualifiedAccess>]
    type FipcStream =
        | PipeServer of NamedPipeServerStream
        | PipeClient of NamedPipeClientStream
        | Network of NetworkStream
 
        member fs.Close() =
            match fs with
            | PipeServer npss -> npss.Close()
            | PipeClient npcs -> npcs.Close()
            | _ -> ()
        
        member fs.GetStream() =
            match fs with
            | PipeServer npss -> npss :> Stream
            | PipeClient npcs -> npcs :> Stream
            | Network ns -> ns :> Stream

        member fs.IsConnected() =
            match fs with
            | PipeServer npss -> npss.IsConnected
            | PipeClient npcs -> npcs.IsConnected
            | Network ns ->
                //ns.WriteByte(0uy)
                ns.Socket.Connected

    type FipcMessageHeader =
        { Length: int
          Correlation: FipcCorrelation }
        static member HeaderLength = 16

        static member Create(length: int, correlation: byte array) =
            { Length = length
              Correlation = { Raw = correlation } }

        static member HeartBeat(correlation: byte array) =
            { Length = 0
              Correlation = { Raw = correlation } }
        
        static member TryParse(buffer: byte array, hasMagicBytes: bool) =
            let parse (headerBuffer: byte array) =
                let len = BitConverter.ToInt32(headerBuffer, 0)
                let corr = headerBuffer.[4..]

                { Length = len
                  Correlation = { Raw = corr } }

            match hasMagicBytes, buffer.Length with
            | true, bl when bl >= 16 ->
                match isMagicBytes buffer.[0..3] with
                | true -> parse buffer.[4..] |> Ok
                | false -> Error $"Magic bytes do not match, not a valid message header."
            | true, _ ->
                Error $"Buffer is too short for message head with magic bytes (length: {buffer.Length} required: 16)"
            | false, bl when bl >= 12 -> parse buffer |> Ok
            | false, _ ->
                Error $"Buffer is too short for message head without magic bytes (length: {buffer.Length} required: 12)"

        member mh.Serialize() =
            Array.concat [ magicBytes
                           BitConverter.GetBytes mh.Length
                           mh.Correlation.Raw ]
        
        member mh.IsHeartBeat() =
            mh.Correlation.IsHeartBeat() && mh.Length = 0

    [<RequireQualifiedAccess>]
    type FipcMessageContent =
        | Empty
        | Text of string
        | SerializedJson of string
        | Binary of byte array

        member fmc.Serialize() =
            match fmc with
            | Empty -> Array.empty
            | FipcMessageContent.Text t -> Encoding.UTF8.GetBytes t
            | FipcMessageContent.SerializedJson sj -> Encoding.UTF8.GetBytes sj
            | FipcMessageContent.Binary b -> b

    type FipcMessage =
        { Header: FipcMessageHeader
          Body: FipcMessageContent }

        static member HeartBeat() =
            let correlator = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; |]
            
            { Header = FipcMessageHeader.HeartBeat correlator
              Body = FipcMessageContent.Empty }

        
        static member BytesMessage(data: byte array) =
            let correlator = RandomNumberGenerator.GetBytes(8)

            { Header =
                { Length = data.Length
                  Correlation = { Raw = correlator } }
              Body = FipcMessageContent.Binary data }

        static member StringMessage(message: string) =
            // Serialize the message to get the length.
            let sm = Encoding.UTF8.GetBytes message
            let correlator = RandomNumberGenerator.GetBytes(8)

            { Header =
                { Length = sm.Length
                  Correlation = { Raw = correlator } }
              Body = FipcMessageContent.Text message }

        static member JsonMessage<'T>(data: 'T) =
            let sd = JsonSerializer.Serialize data
            let sm = sd |> Encoding.UTF8.GetBytes

            let correlator = RandomNumberGenerator.GetBytes(8)

            { Header =
                { Length = sm.Length
                  Correlation = { Raw = correlator } }
              Body = FipcMessageContent.SerializedJson sd }

        member msg.Serialize() =
            Array.concat [ msg.Header.Serialize()
                           msg.Body.Serialize() ]
            
        member msg.IsHeartBeat() = msg.Header.IsHeartBeat()

    type FipcConnector =
        { Channel: Channel<FipcMessage> }

        
        static member Create() =
            { Channel = Channel.CreateUnbounded<FipcMessage>() }

        member fc.GetReader() = { ChannelReader = fc.Channel.Reader }

        member fc.GetWriter() = { ChannelWriter = fc.Channel.Writer }

    and FipcConnectionReader =
        private
            { ChannelReader: ChannelReader<FipcMessage> }
        
        member fcw.TryReadMessage() =
            match fcw.ChannelReader.TryRead() with
            | true, msg -> Some msg
            | false, _ -> None
            
        member fcw.GetCompletion() = fcw.ChannelReader.Completion

    and FipcConnectionWriter =
        private
            { ChannelWriter: ChannelWriter<FipcMessage> }

        
        interface IDisposable with
            
            member fcw.Dispose() =
                fcw.Close()
        
        member fcw.TryPostMessage(message: FipcMessage) =
            match fcw.ChannelWriter.TryWrite message with
            | true -> Ok()
            | false -> Error "Failed to post message."
            
        member fcw.Close() =
            fcw.ChannelWriter.Complete()

    type FipcConnectionConfiguration =
        { Id: string
          ChannelType: FipcChannelType
          MaxThreads: int
          ContentType: FipcContentType
          EncryptionType: FipcEncryptionType
          CompressionType: FipcCompressionType
          Key: byte array }