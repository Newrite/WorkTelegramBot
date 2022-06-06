namespace WorkTelegram.Infrastructure

open WorkTelegram.Core
open WorkTelegram.Infrastucture

open Donald
open System.Data
open Microsoft.Data.Sqlite

module Database =

  let private schema =
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
        {Field.OfficeId} INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        {Field.OfficeName} TEXT NOT NULL UNIQUE,
        {Field.IsHidden} BOOL NOT NULL,
        {Field.ManagerId} INTEGER NOT NULL,
        FOREIGN KEY({Field.ManagerId}) REFERENCES {ManagerDto.TableName}({Field.ChatId})
      );

      CREATE TABLE IF NOT EXISTS {EmployerDto.TableName} (
	      {Field.ChatId} INTEGER NOT NULL PRIMARY KEY,
	      first_name TEXT NOT NULL,
	      {Field.LastName} TEXT NOT NULL,
        {Field.IsApproved} BOOL NOT NULL,
        {Field.OfficeId} INTEGER NOT NULL,
        FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId}),
	      FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId})
      );
      
      CREATE TABLE IF NOT EXISTS {DeletionItemDto.TableName} (
        {Field.DeletionId} INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        {Field.ItemName} TEXT NOT NULL,
        {Field.ItemSerial} TEXT DEFAULT(NULL),
        {Field.ItemMac} TEXT DEFAULT(NULL),
        {Field.Count} INTEGER NOT NULL,
        {Field.Date} INTEGER NOT NULL,
        {Field.IsDeletion} BOOL NOT NULL,
        {Field.IsHidden} BOOL NOT NULL,
        {Field.ToLocation} TEXT DEFAULT(NULL),
        {Field.OfficeId} INTEGER NOT NULL,
        {Field.ChatId} INTEGER NOT NULL,
        FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId}),
        FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
      );"

  let createConnection (env: #ILog) databaseName =
    env.Logger.Info "Start create connection to database"

    try
      let connectionString = SqliteConnectionStringBuilder()
      connectionString.DataSource <- databaseName
      connectionString.ForeignKeys <- System.Nullable<_>(true)
      let conn = new SqliteConnection(connectionString.ToString())
      conn.Open()
      env.Logger.Info $"Success create connection to database = {connectionString.ToString()}"
      conn
    with
    | _ ->
      env.Logger.Error $"Failed create connection to database"
      reraise ()

  let initalizationTables (env: #ILog) conn =
    env.Logger.Info $"Execute schema sql script"

    use command = new SqliteCommand(schema, conn)
    let result = command.ExecuteNonQuery()
    env.Logger.Debug $"Schema executed with code: {result}"

  let private stringOrNull (opt: string option) =
    if opt.IsSome then
      SqlType.String opt.Value
    else
      SqlType.Null

  let private genericSelectMany<'a> conn tableName (ofDataReader: IDataReader -> 'a) =
    let sql = $"SELECT * FROM {tableName}"

    Db.newCommand sql conn
    |> Db.query ofDataReader
    |> function
      | Ok list -> list
      | Error _ -> []

  let private genericSelectManyWithWhere<'a>
    conn
    sqlCommand
    sqlParam
    (ofDataReader: IDataReader -> 'a)
    =
    Db.newCommand sqlCommand conn
    |> Db.setParams sqlParam
    |> Db.query ofDataReader
    |> function
      | Ok list -> list
      | Error _ -> []

  let private genericSelectSingle<'a> conn sqlCommand sqlParam (ofDataReader: IDataReader -> 'a) =
    Db.newCommand sqlCommand conn
    |> Db.setParams sqlParam
    |> Db.querySingle ofDataReader
    |> function
      | Ok opt ->
        match opt with
        | Some v -> Ok v
        | None ->
          BusinessError.NotFoundInDatabase
          |> AppError.BusinessError
          |> Error
      | Error err -> err |> AppError.DatabaseError |> Error

  let private transactionSingleExn (conn: IDbConnection) sqlCommand sqlParam =

    use tran = conn.TryBeginTransaction()

    let result =
      Db.newCommand sqlCommand conn
      |> Db.setParams sqlParam
      |> Db.setTransaction tran
      |> Db.exec

    tran.TryCommit()

    match result with
    | Ok _ -> Ok ()
    | Error err -> err |> AppError.DatabaseError |> Error

  let private transactionManyExn (conn: IDbConnection) sqlCommand sqlParam =

    use tran = conn.TryBeginTransaction()

    let result =
      Db.newCommand sqlCommand conn
      |> Db.setTransaction tran
      |> Db.execMany sqlParam

    tran.TryCommit()

    match result with
    | Ok _ -> Ok ()
    | Error err -> err |> AppError.DatabaseError |> Error

  let internal selectMessages conn =
    genericSelectMany<MessageDto> conn MessageDto.TableName MessageDto.ofDataReader

  let internal selectManagers conn =
    genericSelectMany<ManagerDto> conn ManagerDto.TableName ManagerDto.ofDataReader

  let internal selectOffices conn =
    genericSelectMany<OfficeDto> conn OfficeDto.TableName OfficeDto.ofDataReader

  let internal selectEmployers conn =
    genericSelectMany<EmployerDto> conn EmployerDto.TableName EmployerDto.ofDataReader

  let internal selectDeletionItems conn =
    genericSelectMany<DeletionItemDto> conn DeletionItemDto.TableName DeletionItemDto.ofDataReader

  let internal selectMessageByChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"SELECT * FROM {MessageDto.TableName} WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectSingle<MessageDto> conn sqlCommand sqlParam MessageDto.ofDataReader

  let internal selectOfficesByManagerChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"SELECT * FROM {OfficeDto.TableName} WHERE {Field.ManagerId} = (@{Field.ManagerId})"

    let sqlParam = [ Field.ManagerId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectManyWithWhere<OfficeDto> conn sqlCommand sqlParam OfficeDto.ofDataReader

  let internal selectEmployerByChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"SELECT * FROM {EmployerDto.TableName} WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectSingle<EmployerDto> conn sqlCommand sqlParam EmployerDto.ofDataReader

  let internal selectManagerByChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"SELECT * FROM {ManagerDto.TableName} WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectSingle<ManagerDto> conn sqlCommand sqlParam ManagerDto.ofDataReader

  let internal selectOfficeById conn officeId =
    let sqlCommand =
      $"SELECT * FROM {OfficeDto.TableName} WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam = [ Field.OfficeId, SqlType.Int64 officeId ]
    genericSelectSingle<OfficeDto> conn sqlCommand sqlParam OfficeDto.ofDataReader

  let internal selectOfficeByName conn officeName =
    let sqlCommand =
      $"SELECT * FROM {OfficeDto.TableName} WHERE {Field.OfficeName} = (@{Field.OfficeName})"

    let sqlParam = [ Field.OfficeId, SqlType.String officeName ]
    genericSelectSingle<OfficeDto> conn sqlCommand sqlParam OfficeDto.ofDataReader

  let internal selectDeletionItemByTimeTicks conn (ticks: int64) =
    let sqlCommand =
      $"SELECT * FROM {DeletionItemDto.TableName} WHERE {Field.Date} = (@{Field.Date})"

    let sqlParam = [ Field.Date, SqlType.Int64 ticks ]
    genericSelectSingle<DeletionItemDto> conn sqlCommand sqlParam DeletionItemDto.ofDataReader

  let internal insertMessage conn (messageDto: MessageDto) =
    let sqlCommand =
      $"INSERT INTO OR IGNORE {MessageDto.TableName}
        ({Field.ChatId}, {Field.MessageJson})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 messageDto.ChatId
        Field.MessageJson, SqlType.String messageDto.MessageJson ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal insertManager conn (managerDto: ManagerDto) =
    let sqlCommand =
      $"INSERT INTO OR IGNORE {ManagerDto.TableName}
        ({Field.ChatId}, {Field.FirstName}, {Field.LastName})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 managerDto.ChatId
        Field.FirstName, SqlType.String managerDto.FirstName
        Field.LastName, SqlType.String managerDto.LastName ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal insertOffice conn (officeRecord: RecordOffice) =
    let sqlCommand =
      $"INSERT INTO OR IGNORE {OfficeDto.TableName} 
        ({Field.OfficeName}, {Field.IsHidden}, {Field.ManagerId})"

    let sqlParam =
      [ Field.OfficeName, SqlType.String officeRecord.OfficeName
        Field.IsHidden, SqlType.Boolean false
        Field.ManagerId, SqlType.Int64 officeRecord.ManagerChatId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal insertEmployer conn (employerRecord: RecordEmployer) =
    let sqlCommand =
      $"INSERT INTO OR IGNORE {EmployerDto.TableName} 
        ({Field.ChatId}, first_name, {Field.LastName}, {Field.IsApproved}, {Field.OfficeId})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 employerRecord.ChatId
        "first_name", SqlType.String employerRecord.FirstName
        Field.LastName, SqlType.String employerRecord.LastName
        Field.IsApproved, SqlType.Boolean false
        Field.OfficeId, SqlType.Int64 employerRecord.OfficeId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal insertDeletionItem conn (deletionItemRecord: RecordDeletionItem) =
    let sqlCommand =
      $"INSERT INTO OR IGNORE {DeletionItemDto.TableName}
        ({Field.ItemName},
         {Field.ItemSerial},
         {Field.ItemMac},
         {Field.Count},
         {Field.Date},
         {Field.IsDeletion},
         {Field.IsHidden},
         {Field.ToLocation},
         {Field.OfficeId},
         {Field.ChatId})"

    let sqlParam =

      let time = let x = deletionItemRecord.Time in x.Ticks

      [ Field.ItemName, SqlType.String deletionItemRecord.ItemName
        Field.ItemSerial, stringOrNull deletionItemRecord.ItemSerial
        Field.ItemMac, stringOrNull deletionItemRecord.ItemMac
        Field.Count, deletionItemRecord.Count |> int64 |> SqlType.Int64
        Field.Date, SqlType.Int64 time
        Field.IsDeletion, SqlType.Boolean false
        Field.IsHidden, SqlType.Boolean false
        Field.ToLocation, stringOrNull deletionItemRecord.Location
        Field.OfficeId, SqlType.Int64 deletionItemRecord.OfficeId
        Field.ChatId, SqlType.Int64 deletionItemRecord.EmployerChatId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal updateEmployerApprovedByChatId conn (chatIdDto: ChatIdDto) isApproved =
    let sqlCommand =
      $"UPDATE {EmployerDto.TableName} 
        SET {Field.IsApproved} = (@{Field.IsApproved})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId
        Field.IsApproved, SqlType.Boolean isApproved ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal setTrueForDeletionFieldOfOfficeItems conn officeId =
    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET {Field.IsDeletion} = (@{Field.IsDeletion})
        WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam =
      [ Field.IsDeletion, SqlType.Boolean true
        Field.OfficeId, SqlType.Int64 officeId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal setTrueForHiddenFieldOfItem conn deletionId =
    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET {Field.IsHidden} = (@{Field.IsHidden})
        WHERE {Field.DeletionId} = (@{Field.DeletionId})"

    let sqlParam =
      [ Field.IsHidden, SqlType.Boolean true
        Field.DeletionId, SqlType.Int64 deletionId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal updateMessage conn (messageDto: MessageDto) =
    let sqlCommand =
      $"UPDATE {MessageDto.TableName}
        SET {Field.MessageJson} = (@{Field.MessageJson})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.MessageJson, SqlType.String messageDto.MessageJson
        Field.ChatId, SqlType.Int64 messageDto.ChatId ]

    transactionSingleExn conn sqlCommand sqlParam

  let internal deleteOffice conn officeId =
    let sqlCommand =
      $"DELETE FROM {OfficeDto.TableName}
        WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam = [ Field.OfficeId, SqlType.Int64 officeId ]

    transactionSingleExn conn sqlCommand sqlParam
