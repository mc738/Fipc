namespace Fipc.Messaging.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.Sqlite

/// Module generated on 18/01/2022 21:39:57 (utc) via Freql.Sqlite.Tools.
[<RequireQualifiedAccess>]
module Records =
    /// A record representing a row in the table `logs`.
    type Logs =
        { [<JsonPropertyName("messageReference")>] MessageReference: string
          [<JsonPropertyName("logMessage")>] LogMessage: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime
          [<JsonPropertyName("isError")>] IsError: bool
          [<JsonPropertyName("isWarning")>] IsWarning: bool
          [<JsonPropertyName("itemData")>] ItemData: BlobField option }
    
        static member Blank() =
            { MessageReference = String.Empty
              LogMessage = String.Empty
              CreatedOn = DateTime.UtcNow
              IsError = true
              IsWarning = true
              ItemData = None }
    
        static member CreateTableSql() = """
        CREATE TABLE logs (
	message_reference TEXT NOT NULL,
	log_message TEXT NOT NULL,
	created_on TEXT NOT NULL,
	is_error INTEGER NOT NULL,
	is_warning INTEGER NOT NULL,
	item_data BLOB,
	CONSTRAINT logs_FK FOREIGN KEY (message_reference) REFERENCES messages(reference)
)
        """
    
        static member SelectSql() = """
        SELECT
              message_reference,
              log_message,
              created_on,
              is_error,
              is_warning,
              item_data
        FROM logs
        """
    
        static member TableName() = "logs"
    
    /// A record representing a row in the table `messages`.
    type Message =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("readFrom")>] ReadFrom: string
          [<JsonPropertyName("receivedOn")>] ReceivedOn: DateTime
          [<JsonPropertyName("rawMessage")>] RawMessage: BlobField
          [<JsonPropertyName("serial")>] Serial: int64
          [<JsonPropertyName("contentType")>] ContentType: string }
    
        static member Blank() =
            { Reference = String.Empty
              ReadFrom = String.Empty
              ReceivedOn = DateTime.UtcNow
              RawMessage = BlobField.Empty()
              Serial = 0L
              ContentType = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE messages (
	reference TEXT NOT NULL,
	"read_from" TEXT NOT NULL,
	received_on TEXT NOT NULL,
	raw_message BLOB NOT NULL,
	serial INTEGER NOT NULL,
	"content_type" TEXT NOT NULL,
	CONSTRAINT messages_PK PRIMARY KEY (reference),
	CONSTRAINT messages_UN UNIQUE ("read_from",serial),
	CONSTRAINT messages_FK FOREIGN KEY ("read_from") REFERENCES readers(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              reference,
              read_from,
              received_on,
              raw_message,
              serial,
              content_type
        FROM messages
        """
    
        static member TableName() = "messages"
    
    /// A record representing a row in the table `readers`.
    type Reader =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("connectType")>] ConnectType: string
          [<JsonPropertyName("configBlob")>] ConfigBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              ConnectType = String.Empty
              ConfigBlob = BlobField.Empty() }
    
        static member CreateTableSql() = """
        CREATE TABLE readers (
	name TEXT NOT NULL,
	"connect_type" TEXT NOT NULL,
	config_blob BLOB NOT NULL,
	CONSTRAINT readers_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              name,
              connect_type,
              config_blob
        FROM readers
        """
    
        static member TableName() = "readers"
    
    /// A record representing a row in the table `writers`.
    type Writer =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("connectionType")>] ConnectionType: string
          [<JsonPropertyName("configBlob")>] ConfigBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              ConnectionType = String.Empty
              ConfigBlob = BlobField.Empty() }
    
        static member CreateTableSql() = """
        CREATE TABLE writers (
	name TEXT NOT NULL,
	"connection_type" TEXT NOT NULL,
	config_blob BLOB NOT NULL,
	CONSTRAINT writers_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              name,
              connection_type,
              config_blob
        FROM writers
        """
    
        static member TableName() = "writers"
    

/// Module generated on 18/01/2022 21:39:57 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `logs`.
    type NewLogs =
        { [<JsonPropertyName("messageReference")>] MessageReference: string
          [<JsonPropertyName("logMessage")>] LogMessage: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime
          [<JsonPropertyName("isError")>] IsError: bool
          [<JsonPropertyName("isWarning")>] IsWarning: bool
          [<JsonPropertyName("itemData")>] ItemData: BlobField option }
    
        static member Blank() =
            { MessageReference = String.Empty
              LogMessage = String.Empty
              CreatedOn = DateTime.UtcNow
              IsError = true
              IsWarning = true
              ItemData = None }
    
    
    /// A record representing a new row in the table `messages`.
    type NewMessage =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("readFrom")>] ReadFrom: string
          [<JsonPropertyName("receivedOn")>] ReceivedOn: DateTime
          [<JsonPropertyName("rawMessage")>] RawMessage: BlobField
          [<JsonPropertyName("serial")>] Serial: int64
          [<JsonPropertyName("contentType")>] ContentType: string }
    
        static member Blank() =
            { Reference = String.Empty
              ReadFrom = String.Empty
              ReceivedOn = DateTime.UtcNow
              RawMessage = BlobField.Empty()
              Serial = 0L
              ContentType = String.Empty }
    
    
    /// A record representing a new row in the table `readers`.
    type NewReader =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("connectType")>] ConnectType: string
          [<JsonPropertyName("configBlob")>] ConfigBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              ConnectType = String.Empty
              ConfigBlob = BlobField.Empty() }
    
    
    /// A record representing a new row in the table `writers`.
    type NewWriter =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("connectionType")>] ConnectionType: string
          [<JsonPropertyName("configBlob")>] ConfigBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              ConnectionType = String.Empty
              ConfigBlob = BlobField.Empty() }
    
    
/// Module generated on 18/01/2022 21:39:57 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Operations =

    let buildSql (lines: string list) = lines |> String.concat Environment.NewLine

    /// Select a `Records.Logs` from the table `logs`.
    /// Internally this calls `context.SelectSingleAnon<Records.Logs>` and uses Records.Logs.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectLogsRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectLogsRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Logs.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Logs>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Logs>` and uses Records.Logs.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectLogsRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectLogsRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Logs.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Logs>(sql, parameters)
    
    let insertLogs (context: SqliteContext) (parameters: Parameters.NewLogs) =
        context.Insert("logs", parameters)
    
    /// Select a `Records.Message` from the table `messages`.
    /// Internally this calls `context.SelectSingleAnon<Records.Message>` and uses Records.Message.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectMessageRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectMessageRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Message.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Message>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Message>` and uses Records.Message.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectMessageRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectMessageRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Message.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Message>(sql, parameters)
    
    let insertMessage (context: SqliteContext) (parameters: Parameters.NewMessage) =
        context.Insert("messages", parameters)
    
    /// Select a `Records.Reader` from the table `readers`.
    /// Internally this calls `context.SelectSingleAnon<Records.Reader>` and uses Records.Reader.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectReaderRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectReaderRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Reader.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Reader>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Reader>` and uses Records.Reader.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectReaderRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectReaderRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Reader.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Reader>(sql, parameters)
    
    let insertReader (context: SqliteContext) (parameters: Parameters.NewReader) =
        context.Insert("readers", parameters)
    
    /// Select a `Records.Writer` from the table `writers`.
    /// Internally this calls `context.SelectSingleAnon<Records.Writer>` and uses Records.Writer.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectWriterRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectWriterRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Writer.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Writer>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Writer>` and uses Records.Writer.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectWriterRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectWriterRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Writer.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Writer>(sql, parameters)
    
    let insertWriter (context: SqliteContext) (parameters: Parameters.NewWriter) =
        context.Insert("writers", parameters)
    