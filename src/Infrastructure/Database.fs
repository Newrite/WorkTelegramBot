namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

open Donald
open System.Data
open Microsoft.Data.Sqlite

#nowarn "64"

module Database =

  let private schema =
    $@"CREATE TABLE IF NOT EXISTS {ChatIdDto.TableName} (
        {Field.ChatId} INTEGER NOT NULL PRIMARY KEY
      );
  
      CREATE TABLE IF NOT EXISTS {TelegramMessageDto.TableName} (
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

  let createConnection env databaseName =
    Logger.info env "Start create connection to database"

    try
      let connectionString = SqliteConnectionStringBuilder()
      connectionString.DataSource <- databaseName
      connectionString.ForeignKeys <- System.Nullable<_>(true)
      let conn = new SqliteConnection(connectionString.ToString())
      conn.Open()
      Logger.info env $"Success create connection to database = {connectionString.ToString()}"
      conn
    with
    | _ ->
      Logger.info env $"Failed create connection to database"
      reraise ()

  let initTables (env: #IDb) =
    Logger.info env "Execute schema sql script"

    use command = new SqliteCommand(schema, env.Db.Conn)
    let result = command.ExecuteNonQuery()
    Logger.debug env $"Schema executed with code: {result}"
    result

  let private stringOrNull (opt: string option) =
    if opt.IsSome then
      SqlType.String opt.Value
    else
      SqlType.Null

  let private genericSelectMany<'a> (env: #IDb) tableName (ofDataReader: IDataReader -> 'a) =
    let sql = $"SELECT * FROM {tableName}"

    Db.newCommand sql env.Db.Conn
    |> Db.query ofDataReader
    |> function
      | Ok list -> list |> Ok
      | Error err -> err |> AppError.DatabaseError |> Error

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
      | Ok list -> list |> Ok
      | Error err -> err |> AppError.DatabaseError |> Error

  let private genericSelectSingle<'a>
    (env: #IDb)
    sqlCommand
    sqlParam
    (ofDataReader: IDataReader -> 'a)
    =
    Db.newCommand sqlCommand env.Db.Conn
    |> Db.setParams sqlParam
    |> Db.querySingle ofDataReader
    |> function
      | Ok opt ->
        match opt with
        | Some v -> Ok v
        | None ->
          BusinessError.NotFoundInDatabase(typedefof<'a>)
          |> AppError.BusinessError
          |> Error
      | Error err -> err |> AppError.DatabaseError |> Error

  let private transactionSingleExn (env: #IDb) sqlCommand sqlParam =

    use tran = env.Db.Conn.TryBeginTransaction()

    let result =
      Db.newCommand sqlCommand env.Db.Conn
      |> Db.setParams sqlParam
      |> Db.setTransaction tran
      |> Db.exec

    tran.TryCommit()

    match result with
    | Ok _ -> Ok()
    | Error err -> err |> AppError.DatabaseError |> Error

  let private transactionManyExn (env: #IDb) sqlCommand sqlParam =

    use tran = env.Db.Conn.TryBeginTransaction()

    let result =
      Db.newCommand sqlCommand env.Db.Conn
      |> Db.setTransaction tran
      |> Db.execMany sqlParam

    tran.TryCommit()

    match result with
    | Ok _ -> Ok()
    | Error err -> err |> AppError.DatabaseError |> Error

  let selectTelegramMessages env =
    genericSelectMany<TelegramMessageDto>
      env
      TelegramMessageDto.TableName
      TelegramMessageDto.ofDataReader

  let selectManagers env =
    genericSelectMany<ManagerDto> env ManagerDto.TableName ManagerDto.ofDataReader

  let selectOffices env =
    genericSelectMany<OfficeDto> env OfficeDto.TableName OfficeDto.ofDataReader

  let selectEmployers env =
    genericSelectMany<EmployerDto> env EmployerDto.TableName EmployerDto.ofDataReader

  let selectDeletionItems env =
    genericSelectMany<DeletionItemDto> env DeletionItemDto.TableName DeletionItemDto.ofDataReader

  let insertTelegramMessage env (messageDto: TelegramMessageDto) =
    let sqlCommand =
      $"INSERT OR IGNORE INTO {TelegramMessageDto.TableName}
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

  let insertOffice env (officeDto: OfficeDto) =
    let sqlCommand =
      $"INSERT OR IGNORE INTO {OfficeDto.TableName} 
        ({Field.OfficeId}, {Field.OfficeName}, {Field.IsHidden}, {Field.ManagerId})
        VALUES
        (@{Field.OfficeId}, @{Field.OfficeName}, @{Field.IsHidden}, @{Field.ManagerId})"

    let sqlParam =
      [ Field.OfficeId, SqlType.Guid officeDto.OfficeId
        Field.OfficeName, SqlType.String officeDto.OfficeName
        Field.IsHidden, SqlType.Boolean officeDto.IsHidden
        Field.ManagerId, SqlType.Int64 officeDto.ManagerId ]

    transactionSingleExn env sqlCommand sqlParam

  let insertEmployer env (employerDto: EmployerDto) =
    let sqlCommand =
      $"INSERT OR IGNORE INTO {EmployerDto.TableName} 
        ({Field.ChatId}, first_name, {Field.LastName}, {Field.IsApproved}, {Field.OfficeId})
        VALUES
        (@{Field.ChatId}, @first_name, @{Field.LastName}, @{Field.IsApproved}, @{Field.OfficeId})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 employerDto.ChatId
        Field.FirstName, SqlType.String employerDto.FirstName
        Field.LastName, SqlType.String employerDto.LastName
        Field.IsApproved, SqlType.Boolean employerDto.IsApproved
        Field.OfficeId, SqlType.Guid employerDto.OfficeId ]

    transactionSingleExn env sqlCommand sqlParam

  let insertDeletionItem env (deletionItemDto: DeletionItemDto) =
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


      [ Field.DeletionId, SqlType.Guid deletionItemDto.DeletionId
        Field.ItemName, SqlType.String deletionItemDto.ItemName
        Field.ItemSerial, stringOrNull deletionItemDto.ItemSerial
        Field.ItemMac, stringOrNull deletionItemDto.ItemMac
        Field.Count, deletionItemDto.Count |> int64 |> SqlType.Int64
        Field.Date, SqlType.Int64 deletionItemDto.Date
        Field.IsDeletion, SqlType.Boolean false
        Field.IsHidden, SqlType.Boolean false
        Field.ToLocation, stringOrNull deletionItemDto.ToLocation
        Field.OfficeId, SqlType.Guid deletionItemDto.OfficeId
        Field.ChatId, SqlType.Int64 deletionItemDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let updateEmployerApprovedByChatId env (chatIdDto: ChatIdDto) isApproved =
    let sqlCommand =
      $"UPDATE {EmployerDto.TableName} 
        SET {Field.IsApproved} = (@{Field.IsApproved})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId
        Field.IsApproved, SqlType.Boolean isApproved ]

    transactionSingleExn env sqlCommand sqlParam

  let deletionDeletionitemsOfOffice env officeId =

    let ticksInDay = 864000000000L
    let currentTicks = let a = System.DateTime.Now in a.Ticks

    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET {Field.IsDeletion} = (@{Field.IsDeletion})
        WHERE {Field.OfficeId} = (@{Field.OfficeId}) AND ({currentTicks} - {Field.Date}) > {ticksInDay}"

    let sqlParam =
      [ Field.IsDeletion, SqlType.Boolean true
        Field.OfficeId, SqlType.Guid officeId ]

    transactionSingleExn env sqlCommand sqlParam

  let hideDeletionItem env deletionId =

    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET {Field.IsHidden} = (@{Field.IsHidden})
        WHERE {Field.DeletionId} = (@{Field.DeletionId})"

    let sqlParam =
      [ Field.IsHidden, SqlType.Boolean true
        Field.DeletionId, SqlType.Guid deletionId ]

    transactionSingleExn env sqlCommand sqlParam

  let updateTelegramMessage env (messageDto: TelegramMessageDto) =
    let sqlCommand =
      $"UPDATE {TelegramMessageDto.TableName}
        SET {Field.MessageJson} = (@{Field.MessageJson})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.MessageJson, SqlType.String messageDto.MessageJson
        Field.ChatId, SqlType.Int64 messageDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let deleteOffice env officeId =
    let sqlCommand =
      $"DELETE FROM {OfficeDto.TableName}
        WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam = [ Field.OfficeId, SqlType.Guid officeId ]

    transactionSingleExn env sqlCommand sqlParam

  let deleteTelegramMessage env (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"DELETE FROM {TelegramMessageDto.TableName}
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let insertChatId env (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"INSERT OR IGNORE INTO {ChatIdDto.TableName} ({Field.ChatId}) VALUES (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let IDbBuilder conn =
    { new IDb with
        member _.Db =
          { new IDatabase with
              member _.Conn = conn } }
