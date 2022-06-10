#r "nuget: Donald"
#r "nuget: Microsoft.Data.Sqlite"

open Donald
open System.Data
open Microsoft.Data.Sqlite

module Field =
  [<Literal>]
  let ChatId = "chat_id"

  [<Literal>]
  let MessageJson = "message_json"

  [<Literal>]
  let FirstName = "first_name"

  [<Literal>]
  let LastName = "last_name"

  [<Literal>]
  let OfficeId = "office_id"

  [<Literal>]
  let OfficeName = "office_name"

  [<Literal>]
  let IsHidden = "is_hidden"

  [<Literal>]
  let ManagerId = "manager_id"

  [<Literal>]
  let IsApproved = "is_approved"

  [<Literal>]
  let DeletionId = "deletion_id"

  [<Literal>]
  let ItemName = "item_name"

  [<Literal>]
  let ItemSerial = "item_serial"

  [<Literal>]
  let ItemMac = "item_mac"

  [<Literal>]
  let Count = "count"

  [<Literal>]
  let Date = "date"

  [<Literal>]
  let IsDeletion = "is_deletion"

  [<Literal>]
  let ToLocation = "to_location"

type ChatIdDto = { ChatId: int64 }

[<RequireQualifiedAccess>]
module ChatIdDto =

  let ofDataReader (rd: IDataReader) = { ChatId = rd.ReadInt64 Field.ChatId }

  [<Literal>]
  let TableName = "chat_id_table"

type MessageDto = { ChatId: int64; MessageJson: string }

[<RequireQualifiedAccess>]
module MessageDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      MessageJson = rd.ReadString Field.MessageJson }

  [<Literal>]
  let TableName = "message"

type ManagerDto =
  { ChatId: int64
    FirstName: string
    LastName: string }

[<RequireQualifiedAccess>]
module ManagerDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString "firt_name"
      LastName = rd.ReadString Field.LastName }

  [<Literal>]
  let TableName = "manager"

type OfficeDto =
  { OfficeId: int64
    OfficeName: string
    IsHidden: bool
    ManagerId: int64 }
  
type OfficeDtoNew =
  { OfficeId: System.Guid
    OfficeName: string
    IsHidden: bool
    ManagerId: int64 }

[<RequireQualifiedAccess>]
module OfficeDto =

  let ofDataReader (rd: IDataReader): OfficeDto =
    { OfficeId = rd.ReadInt64 Field.OfficeId
      OfficeName = rd.ReadString Field.OfficeName
      IsHidden = rd.ReadBoolean Field.IsHidden
      ManagerId = rd.ReadInt64 Field.ManagerId }
    
  let ofDataReaderNew (rd: IDataReader): OfficeDtoNew =
    { OfficeId = rd.ReadGuid Field.OfficeId
      OfficeName = rd.ReadString Field.OfficeName
      IsHidden = rd.ReadBoolean Field.IsHidden
      ManagerId = rd.ReadInt64 Field.ManagerId }

  [<Literal>]
  let TableName = "office"

type EmployerDto =
  { ChatId: int64
    FirstName: string
    LastName: string
    IsApproved: bool
    OfficeId: int64 }
  
type EmployerDtoNew =
  { ChatId: int64
    FirstName: string
    LastName: string
    IsApproved: bool
    OfficeId: System.Guid }

[<RequireQualifiedAccess>]
module EmployerDto =

  let ofDataReader (rd: IDataReader): EmployerDto =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString Field.FirstName
      LastName = rd.ReadString Field.LastName
      IsApproved = rd.ReadBoolean Field.IsApproved
      OfficeId = rd.ReadInt64 Field.OfficeId }
    
  let ofDataReaderNew (rd: IDataReader): EmployerDtoNew =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString Field.FirstName
      LastName = rd.ReadString Field.LastName
      IsApproved = rd.ReadBoolean Field.IsApproved
      OfficeId = rd.ReadGuid Field.OfficeId }

  [<Literal>]
  let TableName = "employer"

type DeletionItemDto =
  { DeletionId: int64
    ItemName: string
    ItemSerial: string option
    ItemMac: string option
    Count: int64
    Date: int64
    IsDeletion: bool
    IsHidden: bool
    ToLocation: string option
    OfficeId: int64
    ChatId: int64 }
  
type DeletionItemDtoNew =
  { DeletionId: System.Guid
    ItemName: string
    ItemSerial: string option
    ItemMac: string option
    Count: int64
    Date: int64
    IsDeletion: bool
    IsHidden: bool
    ToLocation: string option
    OfficeId: System.Guid
    ChatId: int64 }

[<RequireQualifiedAccess>]
module DeletionItemDto =

  let ofDataReader (rd: IDataReader): DeletionItemDto =
    { DeletionId = rd.ReadInt64 Field.DeletionId
      ItemName = rd.ReadString Field.ItemName
      ItemSerial = rd.ReadStringOption Field.ItemSerial
      ItemMac = rd.ReadStringOption Field.ItemMac
      Count = rd.ReadInt64 Field.Count
      Date = rd.ReadInt64 Field.Date
      IsDeletion = rd.ReadBoolean Field.IsDeletion
      IsHidden = rd.ReadBoolean Field.IsHidden
      ToLocation = rd.ReadStringOption Field.ToLocation
      OfficeId = rd.ReadInt64 Field.OfficeId
      ChatId = rd.ReadInt64 Field.ChatId }
    
  let ofDataReaderNew (rd: IDataReader): DeletionItemDtoNew =
    { DeletionId = rd.ReadGuid Field.DeletionId
      ItemName = rd.ReadString Field.ItemName
      ItemSerial = rd.ReadStringOption Field.ItemSerial
      ItemMac = rd.ReadStringOption Field.ItemMac
      Count = rd.ReadInt64 Field.Count
      Date = rd.ReadInt64 Field.Date
      IsDeletion = rd.ReadBoolean Field.IsDeletion
      IsHidden = rd.ReadBoolean Field.IsHidden
      ToLocation = rd.ReadStringOption Field.ToLocation
      OfficeId = rd.ReadGuid Field.OfficeId
      ChatId = rd.ReadInt64 Field.ChatId }
    
  [<Literal>]
  let TableName = "deletion_items"
 
let private newSchema =
  $@"CREATE TABLE IF NOT EXISTS {ChatIdDto.TableName} (
      {Field.ChatId} INTEGER NOT NULL PRIMARY KEY
    );

    CREATE TABLE IF NOT EXISTS {MessageDto.TableName} (
      {Field.ChatId} INTEGER NOT NULL PRIMARY KEY,
      {Field.MessageJson} TEXT NOT NULL,
      FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
    );
  
    CREATE TABLE IF NOT EXISTS {ManagerDto.TableName} (
      {Field.ChatId} INTEGER NOT NULL PRIMARY KEY,
      {Field.FirstName} TEXT NOT NULL,
      {Field.LastName} TEXT NOT NULL,
      FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
    );
  
    CREATE TABLE IF NOT EXISTS {OfficeDto.TableName} (
      {Field.OfficeId} GUID NOT NULL PRIMARY KEY,
      {Field.OfficeName} TEXT NOT NULL UNIQUE,
      {Field.IsHidden} BOOL NOT NULL,
      {Field.ManagerId} INTEGER NOT NULL,
      FOREIGN KEY({Field.ManagerId}) REFERENCES {ManagerDto.TableName}({Field.ChatId})
    );

    CREATE TABLE IF NOT EXISTS {EmployerDto.TableName} (
      {Field.ChatId} INTEGER NOT NULL PRIMARY KEY,
      {Field.FirstName} TEXT NOT NULL,
      {Field.LastName} TEXT NOT NULL,
      {Field.IsApproved} BOOL NOT NULL,
      {Field.OfficeId} GUID NOT NULL,
      FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId}),
      FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId})
    );
    
    CREATE TABLE IF NOT EXISTS {DeletionItemDto.TableName} (
      {Field.DeletionId} GUID NOT NULL PRIMARY KEY,
      {Field.ItemName} TEXT NOT NULL,
      {Field.ItemSerial} TEXT DEFAULT(NULL),
      {Field.ItemMac} TEXT DEFAULT(NULL),
      {Field.Count} INTEGER NOT NULL,
      {Field.Date} INTEGER NOT NULL,
      {Field.IsDeletion} BOOL NOT NULL,
      {Field.IsHidden} BOOL NOT NULL,
      {Field.ToLocation} TEXT DEFAULT(NULL),
      {Field.OfficeId} GUID NOT NULL,
      {Field.ChatId} INTEGER NOT NULL,
      FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId}),
      FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
    );"

  
let genericSelectMany<'a> conn tableName (ofDataReader: IDataReader -> 'a) =
  let sql = $"SELECT * FROM {tableName}"
  Db.newCommand sql conn
  |> Db.query ofDataReader
  |> function
    | Ok list -> list
    | Error err ->
      printfn "Error when select: %A" err
      []
      
let transactionSingleExn (conn: IDbConnection) sqlCommand sqlParam =
  use tran = conn.TryBeginTransaction()
  let result =
    Db.newCommand sqlCommand conn
    |> Db.setParams sqlParam
    |> Db.setTransaction tran
    |> Db.exec
  tran.TryCommit()
  match result with
  | Ok _ -> printfn "Transaction successfull complete"
  | Error err -> printfn "Error when transaction: %A" err
  
let stringOrNull (opt: string option) =
  if opt.IsSome then
    SqlType.String opt.Value
  else
    SqlType.Null
  
let insertMessage env (messageDto: MessageDto) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {MessageDto.TableName}
      ({Field.ChatId}, {Field.MessageJson})
      VALUES
      (@{Field.ChatId}, @{Field.MessageJson})"

  let sqlParam =
    [ Field.ChatId, SqlType.Int64 messageDto.ChatId
      Field.MessageJson, SqlType.String messageDto.MessageJson ]

  transactionSingleExn env sqlCommand sqlParam

let insertManager env (managerDto: ManagerDto) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {ManagerDto.TableName}
      ({Field.ChatId}, {Field.FirstName}, {Field.LastName})
      VALUES
      (@{Field.ChatId}, @{Field.FirstName}, @{Field.LastName})"

  let sqlParam =
    [ Field.ChatId, SqlType.Int64 managerDto.ChatId
      Field.FirstName, SqlType.String managerDto.FirstName
      Field.LastName, SqlType.String managerDto.LastName ]

  transactionSingleExn env sqlCommand sqlParam

let insertOffice env (officeRecord: OfficeDtoNew) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {OfficeDto.TableName} 
      ({Field.OfficeId},{Field.OfficeName}, {Field.IsHidden}, {Field.ManagerId})
      VALUES
      (@{Field.OfficeId}, @{Field.OfficeName}, @{Field.IsHidden}, @{Field.ManagerId})"

  let sqlParam =
    [ Field.OfficeId, SqlType.Guid officeRecord.OfficeId
      Field.OfficeName, SqlType.String officeRecord.OfficeName
      Field.IsHidden, SqlType.Boolean officeRecord.IsHidden
      Field.ManagerId, SqlType.Int64 officeRecord.ManagerId ]

  transactionSingleExn env sqlCommand sqlParam

let insertEmployer env (employerRecord: EmployerDtoNew) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {EmployerDto.TableName} 
      ({Field.ChatId}, {Field.FirstName}, {Field.LastName}, {Field.IsApproved}, {Field.OfficeId})
      VALUES
      (@{Field.ChatId}, @{Field.FirstName}, @{Field.LastName}, @{Field.IsApproved}, @{Field.OfficeId})"

  let sqlParam =
    [ Field.ChatId, SqlType.Int64 employerRecord.ChatId
      Field.FirstName, SqlType.String employerRecord.FirstName
      Field.LastName, SqlType.String employerRecord.LastName
      Field.IsApproved, SqlType.Boolean employerRecord.IsApproved
      Field.OfficeId, SqlType.Guid employerRecord.OfficeId ]

  transactionSingleExn env sqlCommand sqlParam

let insertDeletionItem env (deletionItemRecord: DeletionItemDtoNew) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {DeletionItemDto.TableName}
      ({Field.DeletionId},
       {Field.ItemName},
       {Field.ItemSerial},
       {Field.ItemMac},
       {Field.Count},
       {Field.Date},
       {Field.IsDeletion},
       {Field.IsHidden},
       {Field.ToLocation},
       {Field.OfficeId},
       {Field.ChatId})
       VALUES
      (@{Field.DeletionId},
       @{Field.ItemName},
       @{Field.ItemSerial},
       @{Field.ItemMac},
       @{Field.Count},
       @{Field.Date},
       @{Field.IsDeletion},
       @{Field.IsHidden},
       @{Field.ToLocation},
       @{Field.OfficeId},
       @{Field.ChatId})"

  let sqlParam =

    [ Field.DeletionId, SqlType.Guid deletionItemRecord.DeletionId
      Field.ItemName, SqlType.String deletionItemRecord.ItemName
      Field.ItemSerial, stringOrNull deletionItemRecord.ItemSerial
      Field.ItemMac, stringOrNull deletionItemRecord.ItemMac
      Field.Count, deletionItemRecord.Count |> int64 |> SqlType.Int64
      Field.Date, SqlType.Int64 deletionItemRecord.Date
      Field.IsDeletion, SqlType.Boolean deletionItemRecord.IsDeletion
      Field.IsHidden, SqlType.Boolean deletionItemRecord.IsHidden
      Field.ToLocation, stringOrNull deletionItemRecord.ToLocation
      Field.OfficeId, SqlType.Guid deletionItemRecord.OfficeId
      Field.ChatId, SqlType.Int64 deletionItemRecord.ChatId ]

  transactionSingleExn env sqlCommand sqlParam
  
let insertChatId env (chatIdDto: ChatIdDto) =
  let sqlCommand =
    $"INSERT OR IGNORE INTO {ChatIdDto.TableName} ({Field.ChatId}) VALUES (@{Field.ChatId})"

  let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]

  transactionSingleExn env sqlCommand sqlParam

let createConnection databaseName =
  printfn "Start create connection to database"
  try
    let connectionString = SqliteConnectionStringBuilder()
    connectionString.DataSource <- databaseName
    connectionString.ForeignKeys <- System.Nullable<_>(true)
    let conn = new SqliteConnection(connectionString.ToString())
    conn.Open()
    printfn $"Success create connection to database = {connectionString.ToString()}"
    conn
  with
  | _ ->
    printfn "Failed create connection to database"
    reraise ()

let initSchema conn schema =
  printfn "Execute schema sql script"

  use command = new SqliteCommand(schema, conn)
  let result = command.ExecuteNonQuery()
  printfn $"Schema executed with code: {result}"

let databaseName = "WorkBotDatabase.sqlite3"
let databaseNameNew = "WorkBotDatabaseNew.sqlite3"

let oldBaseConnect = createConnection databaseName
let newBaseConnect = createConnection databaseNameNew

initSchema newBaseConnect newSchema

genericSelectMany<ChatIdDto> oldBaseConnect ChatIdDto.TableName ChatIdDto.ofDataReader
|> List.iter (insertChatId newBaseConnect)
  
genericSelectMany<MessageDto> oldBaseConnect MessageDto.TableName MessageDto.ofDataReader
|> List.iter (insertMessage newBaseConnect)

genericSelectMany<ManagerDto> oldBaseConnect ManagerDto.TableName ManagerDto.ofDataReader
|> List.iter (insertManager newBaseConnect)

let oldOffices = genericSelectMany<OfficeDto> oldBaseConnect OfficeDto.TableName OfficeDto.ofDataReader

let newOffices =
  oldOffices
  |> List.map (fun oldOffice ->
    let newOffice: OfficeDtoNew =
      let guid = System.Guid.NewGuid()
      { OfficeId = guid
        OfficeName = oldOffice.OfficeName
        IsHidden = oldOffice.IsHidden
        ManagerId = oldOffice.ManagerId }
    newOffice, oldOffice)
  
newOffices
|> List.iter (fun (office, _) -> insertOffice newBaseConnect office)

genericSelectMany<EmployerDto> oldBaseConnect EmployerDto.TableName EmployerDto.ofDataReader
|> List.map (fun oldEmployer ->
  let officeGuid =
    let newOffice, _ = newOffices |> List.find (fun (newO, oldO) -> oldO.OfficeId = oldEmployer.OfficeId)
    newOffice.OfficeId
  let newEmployer: EmployerDtoNew =
    { ChatId = oldEmployer.ChatId
      FirstName = oldEmployer.FirstName
      LastName = oldEmployer.LastName
      OfficeId = officeGuid
      IsApproved = oldEmployer.IsApproved }
  newEmployer)
|> List.iter (insertEmployer newBaseConnect)

genericSelectMany<DeletionItemDto> oldBaseConnect DeletionItemDto.TableName DeletionItemDto.ofDataReader
|> List.map (fun oldDi ->
  let officeGuid =
    let newOffice, _ = newOffices |> List.find (fun (newO, oldO) -> oldO.OfficeId = oldDi.OfficeId)
    newOffice.OfficeId
  let newDi: DeletionItemDtoNew =
    let guid = System.Guid.NewGuid()
    { DeletionId = guid
      ItemMac = oldDi.ItemMac
      ItemName = oldDi.ItemName
      ItemSerial = oldDi.ItemSerial
      ToLocation = oldDi.ToLocation
      Date = oldDi.Date
      Count = oldDi.Count
      IsHidden = oldDi.IsHidden
      IsDeletion = oldDi.IsDeletion
      OfficeId = officeGuid
      ChatId = oldDi.ChatId }
  newDi)
|> List.iter (insertDeletionItem newBaseConnect)

oldBaseConnect.Close()
oldBaseConnect.Dispose()
newBaseConnect.Close()
newBaseConnect.Dispose()

printfn "Migrate script finish"