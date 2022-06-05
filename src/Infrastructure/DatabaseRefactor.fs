namespace WorkTelegram.Infrastructure

open WorkTelegram.Core
open WorkTelegram.Infrastucture

open Donald
open System.Data
open FSharp.UMX
open FSharp.Json
open Microsoft.Data.Sqlite

#nowarn "0039"

module DatabaseNew =

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

  let createConnection (logger: #ILogger) databaseName =
    logger.Debug "Start create connection to database"

    try
      let connectionString = SqliteConnectionStringBuilder()
      connectionString.DataSource <- databaseName
      connectionString.ForeignKeys <- System.Nullable<_>(true)
      let conn = new SqliteConnection(connectionString.ToString())
      conn.Open()
      logger.Info $"Success create connection to database = {connectionString.ToString()}"
      conn
    with
    | _ ->
      logger.Error $"Failed create connection to database"
      reraise ()

  let initalizationTables (logger: #ILogger) (conn: #IDatabase)  =
    logger.Info $"Execute schema sql script"
    use command = new SqliteCommand(schema, conn)
    let result = command.ExecuteNonQuery()
    logger.Debug $"Schema executed with code: {result}"

  let private genericSelectMany<'a> conn tableName (ofDataReader: IDataReader -> 'a) =
      let sql = $"SELECT * FROM {tableName}"
      Db.newCommand sql conn
      |> Db.query ofDataReader
      |> function
      | Ok list -> list
      | Error _ -> []

  let private genericSelectManyWithWhere<'a> conn sqlCommand sqlParam (ofDataReader: IDataReader -> 'a) =
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
        | Some v ->
          Ok v
        | None ->
          BusinessError.NotFoundInDatabase
          |> AppError.BusinessError
          |> Error
      | Error err ->
        err 
        |> AppError.DatabaseError
        |> Error

  let private transactionSingleExn (conn: IDbConnection) sqlCommand sqlParam =
    
    let tran = conn.TryBeginTransaction()

    let result =
      Db.newCommand sqlCommand conn
      |> Db.setParams sqlParam
      |> Db.setTransaction tran
      |> Db.exec

    tran.TryCommit()

    result

  let private transactionManyExn (conn: IDbConnection) sqlCommand sqlParam =

    let tran = conn.TryBeginTransaction()

    let param: RawDbParams list = 
      [ "full_name", SqlType.String "John Doe"
        "full_name", SqlType.String "Jane Doe" ]

    let result =
      Db.newCommand sqlCommand conn
      |> Db.setTransaction tran
      |> Db.execMany param

    tran.TryCommit()

    result

  let selectManagers conn =
    genericSelectMany<ManagerDto> conn ManagerDto.TableName ManagerDto.ofDataReader

  let selectOffices conn =
    genericSelectMany<OfficeDto> conn OfficeDto.TableName OfficeDto.ofDataReader

  let selectEmployers conn =
    genericSelectMany<EmployerDto> conn EmployerDto.TableName EmployerDto.ofDataReader

  let selectDeletionItems conn =
    genericSelectMany<DeletionItemDto> conn DeletionItemDto.TableName DeletionItemDto.ofDataReader

  let selectOfficesByManagerChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand = $"SELECT * FROM {OfficeDto.TableName} WHERE {Field.ManagerId} = (@{Field.ManagerId})"
    let sqlParam = [ Field.ManagerId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectManyWithWhere<OfficeDto> conn sqlCommand sqlParam OfficeDto.ofDataReader

  let selectEmployerByChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand = $"SELECT * FROM {EmployerDto.TableName} WHERE {Field.ChatId} = (@{Field.ChatId})"
    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectSingle<EmployerDto> conn sqlCommand sqlParam EmployerDto.ofDataReader

  let selectManagerByChatId conn (chatIdDto: ChatIdDto) =
    let sqlCommand = $"SELECT * FROM {ManagerDto.TableName} WHERE {Field.ChatId} = (@{Field.ChatId})"
    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]
    genericSelectSingle<ManagerDto> conn sqlCommand sqlParam ManagerDto.ofDataReader

  let selectManagerOrEmployerByChatId conn (chatIdDto: ChatIdDto) =
    let employer = selectEmployerByChatId conn chatIdDto
    match employer with
    | Ok e -> e |> Left |> Ok
    | Error firstErr ->
      let manager = selectManagerByChatId conn chatIdDto
      match manager with
      | Ok m -> m |> Right |> Ok
      | Error secondErr -> (firstErr, secondErr) |> Error

  let prototype (employersDto: EmployerDto list) (managersDto: ManagerDto list) (officesDto: OfficeDto list) =
    let findManager id: ManagerDto =
        List.find (fun (m: ManagerDto) -> id = m.ChatId) managersDto
    let findOffice id: OfficeDto =
        List.find (fun (o: OfficeDto) -> id = o.OfficeId) officesDto
    let managers = List.map ManagerDto.toDomain managersDto
    let offices =
      List.map (fun (o: OfficeDto)->
        let manager = findManager o.OfficeId 
        OfficeDto.toDomain o (findManager o.OfficeId) ) officesDto
    let employers =
      List.map (fun (e: EmployerDto) ->
        let office = findOffice e.OfficeId
        let manager = findManager office.ManagerId
        EmployerDto.toDomain  office manager e) employersDto
    (managers, offices, employers)
