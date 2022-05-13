namespace Fipc.Messaging

open System
open System.IO
open Fipc.Core.Common
open Fipc.Messaging.Persistence
open Freql.Core.Common.Types
open Freql.Sqlite

module Infrastructure =

    module Internal =

        let saveMessage (ctx: SqliteContext) (from: string) (message: FipcMessage) =
            use ms = new MemoryStream(message.Serialize())

            ctx.ExecuteInTransaction
                (fun t ->
                    let serial =
                        Operations.buildSql [ Records.Message.SelectSql()
                                              "WHERE read_from = @0 ORDER BY serial DESC" ]
                        |> fun sql -> t.SelectSingleAnon<Records.Message>(sql, [ from ])
                        |> Option.bind (fun m -> m.Serial + 1L |> Some)
                        |> Option.defaultValue 1L

                    ({ Reference = message.Header.Correlation.ToBase64()
                       ReadFrom = from
                       ReceivedOn = DateTime.UtcNow
                       RawMessage = BlobField.FromStream ms
                       Serial = serial
                       ContentType =
                           match message.Body with
                           | FipcMessageContent.Binary _ -> "binary"
                           | FipcMessageContent.Text _ -> "text"
                           | FipcMessageContent.SerializedJson _ -> "json" }: Parameters.NewMessage)
                    |> Operations.insertMessage t)

    /// A FipcMessage wrapped with metadata.
    type WrappedMessage = { From: string; Message: FipcMessage }

    type MessageBus =
        { Readers: Map<string, FipcConnectionReader>
          Writers: Map<string, FipcConnectionWriter>
          Handler: WrappedMessage -> Result<MessageBusOperation list, string>
          OperationsResultHandler: MessageResult -> unit
          Delay: int
           }

        static member Default() =
            { Readers = Map.empty
              Writers = Map.empty
              Handler = fun _ -> Ok []
              OperationsResultHandler = fun _ -> ()
              Delay = 1000 }

        member mb.RunOnce(ctx: SqliteContext) =
            mb.Readers
            |> Map.toList
            |> List.map
                (fun (k, v) ->
                    // Attempt to read a message, is so return the name of the reader and the message.
                    v.TryReadMessage()
                    |> Option.bind (fun r -> (k, r) |> Some))
            |> List.choose id
            |> List.map (fun (k, m) -> { From = k; Message = m })
            |> List.map (fun m -> m, mb.Handler m)
            |> List.map
                (fun (m, r) ->
                    match r with
                    | Ok ops ->
                        // Attempt to handle the operations.
                        ops
                        |> List.map
                            (fun op ->
                                match op with
                                | MessageBusOperation.WriteTo name ->
                                    mb.Writers.TryFind name
                                    |> Option.bind (fun writer -> writer.TryPostMessage(m.Message) |> Some)
                                    |> Option.defaultValue (Error "Writer not found.")
                                | MessageBusOperation.Store ->
                                    Internal.saveMessage ctx m.From m.Message
                                | MessageBusOperation.Drop -> Error "Drop not implemented yet."
                                |> fun r -> { Operation = op; Result = r })
                        |> fun r -> { Message = m; OperationResults = r }
                        |> fun mr -> mb.OperationsResultHandler mr
                    | Error e ->
                        // Handle the error.
                        ())

        /// Run the message bus. This will start a internal listening loop and is blocking.
        member mb.Run(ctx) =
            let rec loop () =
                mb.RunOnce(ctx)
                |> fun _ ->
                    // Delay and clean up.
                    Async.Sleep mb.Delay |> Async.RunSynchronously

                loop ()

            loop ()

    and MessageBusOperation =
        | WriteTo of string
        | Store
        | Drop

    and MessageResult =
        { Message: WrappedMessage
          OperationResults: OperationResult list }

    and OperationResult =
        { Operation: MessageBusOperation
          Result: Result<unit, string> }
