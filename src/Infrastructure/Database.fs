namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

open Donald
open System.Data
open Microsoft.Data.Sqlite

#nowarn "64"

[<Interface>]
type IDatabase =
  abstract Conn: SqliteConnection

[<Interface>]
type IDb =
  abstract Db: IDatabase

module Database =

  exception private DatabaseVersionTableNotExistException of string
  exception private DatabaseUnhandledVersionException of string
  exception private DatabaseVersionTryAddValueException of DbError
  exception private DatabaseVersionHandleVersionException of DbError
  exception private DatabaseTryReadDatabaseVersionTableException of DbError
  exception private DatabaseTryReadOfficeHidenBugFieldTableException of DbError

  let dbConn (env: #IDb) = env.Db.Conn

  let IDbBuilder conn =
    { new IDb with
        member _.Db =
          { new IDatabase with
              member _.Conn = conn } }


  [<Literal>]
  let DbVersionTable = "DB_VERSION"

  [<Literal>]
  let DbVersionField = "VERSION"

  [<Literal>]
  let OfficeHiddenBugFixTable = "OFFICE_HIDDEN_BUG_FIX"

  [<Literal>]
  let FixedField = "FIXED"

  let private versionSchema =
    $@"CREATE TABLE IF NOT EXISTS {DbVersionTable} (
      {DbVersionField} INTEGER NOT NULL
      );"

  let private officeHiddenBugFixSchema =
    $@"CREATE TABLE IF NOT EXISTS {OfficeHiddenBugFixTable} (
      {FixedField} BOOL NOT NULL DEFAULT(false)
      );"

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
        {Field.IsReadyToDeletion} BOOL NOT NULL DEFAULT(false),
        {Field.OfficeId} GUID NOT NULL,
        {Field.ChatId} INTEGER NOT NULL,
        FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId}),
        FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
      );"

  type DatabaseVersions =
    | FirstVersion = 1
    // | SECOND_VERSION = 2
    | ActualVersion = 2

  let private selectVersion envDb envLog =

    let conn = dbConn envDb

    let sqlCommadVersionCheck = $@"SELECT {DbVersionField} FROM {DbVersionTable}"
    Logger.info envLog $"Get version db with command: {sqlCommadVersionCheck}"

    Db.newCommand sqlCommadVersionCheck conn
    |> Db.querySingle (fun rd -> rd.ReadInt32 DbVersionField)
    |> function
      | Ok opt -> opt |> Option.map enum<DatabaseVersions>
      | Error err ->
        Logger.fatal envLog $"Error when try read version: {err}"

        DatabaseTryReadDatabaseVersionTableException(err) |> raise

  let private versionHandler envDb envLog =

    let conn = dbConn envDb

    let rec handler version =
      if version = DatabaseVersions.ActualVersion then
        Logger.info envLog $"Db is actual version: {DatabaseVersions.ActualVersion}"
      elif version = DatabaseVersions.FirstVersion then

        let nextVersion = enum<DatabaseVersions> (int version + 1)
        let nextVersionInt = int nextVersion

        Logger.info
          envLog
          $"Db version is {version}, try update to {nextVersion} : {nextVersionInt} version"

        use tran = conn.TryBeginTransaction()

        let oldDeletionTableName = $"{DeletionItemDto.TableName}_OldV{version}"

        let sqlCommandVersion1 =
          $@"ALTER TABLE {DeletionItemDto.TableName} RENAME TO {oldDeletionTableName};

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
              {Field.IsReadyToDeletion} BOOL NOT NULL DEFAULT(false),
              {Field.OfficeId} GUID NOT NULL,
              {Field.ChatId} INTEGER NOT NULL,
              FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId}),
              FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
            );
            
            INSERT INTO {DeletionItemDto.TableName} 
             ({Field.DeletionId}, {Field.ItemName}, {Field.ItemSerial},
              {Field.ItemMac}, {Field.Count}, {Field.Date},
              {Field.IsDeletion}, {Field.IsHidden}, {Field.ToLocation},
              {Field.OfficeId}, {Field.ChatId})
             SELECT 
              {Field.DeletionId}, {Field.ItemName}, {Field.ItemSerial},
              {Field.ItemMac}, {Field.Count}, {Field.Date},
              {Field.IsDeletion}, {Field.IsHidden}, {Field.ToLocation},
              {Field.OfficeId}, {Field.ChatId}
             FROM {oldDeletionTableName};
             
            UPDATE {DbVersionTable} SET {DbVersionField} = {nextVersionInt};"

        Logger.info
          envLog
          $"Start transaction update from {version} to {nextVersion}, update {DeletionItemDto.TableName}, old table name now {oldDeletionTableName}"

        Db.newCommand sqlCommandVersion1 conn
        |> Db.setTransaction tran
        |> Db.exec
        |> function
          | Ok() ->
            tran.TryCommit()
            Logger.info envLog $"Success update from {version} to {nextVersion}"
            handler nextVersion
          | Error err ->
            Logger.fatal
              envLog
              $"Try rollback, error when try update from {version} to {nextVersion}, erorr: {err}"

            tran.TryRollback()

            DatabaseVersionHandleVersionException(err) |> raise

      //elif version = DATABASE_VERSIONS.SECOND_VERSION then
      //
      //  let oldOfficeTableName = $"{OfficeDto.TableName}_OldV{version}"
      //  let oldEmployerTableName = $"{EmployerDto.TableName}_OldV{version}"
      //  let oldManagerTableName = $"{ManagerDto.TableName}_DeprecetedInV{version}"
      //
      //  let sqlCommandVersion2 =
      //    $@"ALTER TABLE {OfficeDto.TableName} RENAME TO {oldOfficeTableName};
      //
      //       ALTER TABLE {EmployerDto.TableName} RENAME TO {oldEmployerTableName};
      //
      //       ALTER TABLE {ManagerDto.TableName} RENAME TO {oldManagerTableName};
      //
      //       CREATE TABLE IF NOT EXISTS {DeletionItemDto.TableName} (
      //        {Field.DeletionId} GUID NOT NULL PRIMARY KEY,
      //        {Field.ItemName} TEXT NOT NULL,
      //        {Field.ItemSerial} TEXT DEFAULT(NULL),
      //        {Field.ItemMac} TEXT DEFAULT(NULL),
      //        {Field.Count} INTEGER NOT NULL,
      //        {Field.Date} INTEGER NOT NULL,
      //        {Field.IsDeletion} BOOL NOT NULL,
      //        {Field.IsHidden} BOOL NOT NULL,
      //        {Field.ToLocation} TEXT DEFAULT(NULL),
      //        {Field.IsReadyToDeletion} BOOL NOT NULL DEFAULT(false),
      //        {Field.OfficeId} GUID NOT NULL,
      //        {Field.ChatId} INTEGER NOT NULL,
      //        FOREIGN KEY({Field.OfficeId}) REFERENCES {OfficeDto.TableName}({Field.OfficeId}),
      //        FOREIGN KEY({Field.ChatId}) REFERENCES {ChatIdDto.TableName}({Field.ChatId})
      //      );
      //
      //      INSERT INTO {DeletionItemDto.TableName}
      //       ({Field.DeletionId}, {Field.ItemName}, {Field.ItemSerial},
      //        {Field.ItemMac}, {Field.Count}, {Field.Date},
      //        {Field.IsDeletion}, {Field.IsHidden}, {Field.ToLocation},
      //        {Field.OfficeId}, {Field.ChatId})
      //       SELECT
      //        {Field.DeletionId}, {Field.ItemName}, {Field.ItemSerial},
      //        {Field.ItemMac}, {Field.Count}, {Field.Date},
      //        {Field.IsDeletion}, {Field.IsHidden}, {Field.ToLocation},
      //        {Field.OfficeId}, {Field.ChatId}
      //       FROM {oldDeletionTableName};
      //
      //      UPDATE {DbVersionTable} SET {DbVersionField} = {nextVersion};"
      //
      //  ()
      //
      else

        Logger.fatal envLog $"Try handle unhandled version of db: {version}"

        DatabaseUnhandledVersionException(
          $"Unhandled version in database version handler, version: {version}"
        )
        |> raise

    match selectVersion envDb envLog with
    | Some version ->
      Logger.info envLog "Success get version, go handle it"
      handler version
    | None ->

      Logger.fatal envLog $"Error, not found version in database"

      DatabaseVersionTableNotExistException($"Not found version table in database") |> raise

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
    with _ ->
      Logger.info env $"Failed create connection to database"
      reraise ()

  let initTables envDb envLog =

    let conn = dbConn envDb

    Logger.info envLog "Execute schema version script"

    use versionCommand = new SqliteCommand(versionSchema, conn)
    let result = versionCommand.ExecuteNonQuery()

    Logger.debug envLog $"Schema version executed with code: {result}"

    if result >= 0 then

      match selectVersion envDb envLog with
      | Some _ -> ()
      | None ->
        Logger.info envLog "Create value for version table"

        let sqlCommand =
          $@"INSERT OR IGNORE INTO {DbVersionTable}
           ({DbVersionField})
           VALUES
           (@{DbVersionField})"

        let sqlParam = [ DbVersionField, SqlType.Int64(int DatabaseVersions.ActualVersion) ]

        Db.newCommand sqlCommand conn
        |> Db.setParams sqlParam
        |> Db.exec
        |> function
          | Ok() -> Logger.info envLog "Successfull create value for version table"
          | Error err ->
            Logger.fatal envLog $"Error when try add value to version table {err}"
            DatabaseVersionTryAddValueException(err) |> raise

    versionHandler envDb envLog

    Logger.info envLog "Execute office hidden bug fix schema sql script"

    use command = new SqliteCommand(officeHiddenBugFixSchema, conn)
    let result = command.ExecuteNonQuery()
    Logger.debug envLog $"Schema office hidden bug fix executed with code: {result}"

    Logger.info envLog "Execute schema sql script"

    use command = new SqliteCommand(schema, conn)
    let result = command.ExecuteNonQuery()
    Logger.debug envLog $"Schema executed with code: {result}"
    result

  let private stringOrNull (opt: string option) =
    if opt.IsSome then SqlType.String opt.Value else SqlType.Null

  let private genericSelectMany<'a> (env: #IDb) tableName (ofDataReader: IDataReader -> 'a) =

    try

      let sql = $"SELECT * FROM {tableName}"

      Db.newCommand sql env.Db.Conn
      |> Db.query ofDataReader
      |> function
        | Ok list -> list |> Ok
        | Error err -> err |> AppError.DatabaseError |> Error

    with exn ->
      exn |> AppError.Bug |> Error

  let private genericSelectManyWithWhere<'a>
    conn
    sqlCommand
    sqlParam
    (ofDataReader: IDataReader -> 'a)
    =

    try

      Db.newCommand sqlCommand conn
      |> Db.setParams sqlParam
      |> Db.query ofDataReader
      |> function
        | Ok list -> list |> Ok
        | Error err -> err |> AppError.DatabaseError |> Error

    with exn ->
      exn |> AppError.Bug |> Error

  let private genericSelectSingle<'a>
    (env: #IDb)
    sqlCommand
    sqlParam
    (ofDataReader: IDataReader -> 'a)
    =

    try

      Db.newCommand sqlCommand env.Db.Conn
      |> Db.setParams sqlParam
      |> Db.querySingle ofDataReader
      |> function
        | Ok opt ->
          match opt with
          | Some v -> Ok v
          | None ->
            BusinessError.NotFoundInDatabase(typedefof<'a>) |> AppError.BusinessError |> Error
        | Error err -> err |> AppError.DatabaseError |> Error

    with exn ->
      exn |> AppError.Bug |> Error

  let private transactionSingleExn (env: #IDb) sqlCommand sqlParam =

    try

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

    with exn ->
      exn |> AppError.Bug |> Error

  let private transactionManyExn (env: #IDb) sqlCommand sqlParam =

    try

      use tran = env.Db.Conn.TryBeginTransaction()

      let result =
        Db.newCommand sqlCommand env.Db.Conn |> Db.setTransaction tran |> Db.execMany sqlParam

      tran.TryCommit()

      match result with
      | Ok _ -> Ok()
      | Error err -> err |> AppError.DatabaseError |> Error

    with exn ->
      exn |> AppError.Bug |> Error

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

  let selectChatIds env =
    genericSelectMany<ChatIdDto> env ChatIdDto.TableName ChatIdDto.ofDataReader

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

  let updateEmployer env (employerDto: EmployerDto) =
    let sqlCommand =
      $"UPDATE {EmployerDto.TableName} 
        SET 
          {Field.FirstName} = (@{Field.FirstName}),
          {Field.LastName} = (@{Field.LastName}),
          {Field.IsApproved} = (@{Field.IsApproved}),
          {Field.OfficeId} = (@{Field.OfficeId})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.FirstName, SqlType.String employerDto.FirstName
        Field.LastName, SqlType.String employerDto.LastName
        Field.IsApproved, SqlType.Boolean employerDto.IsApproved
        Field.OfficeId, SqlType.Guid employerDto.OfficeId
        Field.ChatId, SqlType.Int64 employerDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let updateOffice env (officeDto: OfficeDto) =

    let sqlCommand =
      $"UPDATE {OfficeDto.TableName}
        SET
          {Field.OfficeName} = (@{Field.OfficeName}),
          {Field.IsHidden} = (@{Field.IsHidden}),
          {Field.ManagerId} = (@{Field.ManagerId})
        WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam =
      [ Field.OfficeName, SqlType.String officeDto.OfficeName
        Field.IsHidden, SqlType.Boolean officeDto.IsHidden
        Field.ManagerId, SqlType.Int64 officeDto.ManagerId
        Field.OfficeId, SqlType.Guid officeDto.OfficeId ]

    transactionSingleExn env sqlCommand sqlParam

  let updateDeletionItems (env: IDb) (deletionItemsDtos: DeletionItemDto list) =

    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET
          {Field.ItemName} = (@{Field.ItemName}),
          {Field.ItemSerial} = (@{Field.ItemSerial}),
          {Field.ItemMac} = (@{Field.ItemMac}),
          {Field.Count} = (@{Field.Count}),
          {Field.Date} = (@{Field.Date}),
          {Field.IsDeletion} = (@{Field.IsDeletion}),
          {Field.IsHidden} = (@{Field.IsHidden}),
          {Field.ToLocation} = (@{Field.ToLocation}),
          {Field.IsReadyToDeletion} = (@{Field.IsReadyToDeletion}),
          {Field.OfficeId} = (@{Field.OfficeId}),
          {Field.ChatId} = (@{Field.ChatId})
        WHERE {Field.DeletionId} = (@{Field.DeletionId})"

    let sqlParamsList: RawDbParams list =
      [ for deletionItemDto in deletionItemsDtos do
          [ Field.ItemName, SqlType.String deletionItemDto.ItemName
            Field.ItemSerial, stringOrNull deletionItemDto.ItemSerial
            Field.ItemMac, stringOrNull deletionItemDto.ItemMac
            Field.Count, SqlType.Int64 deletionItemDto.Count
            Field.Date, SqlType.Int64 deletionItemDto.Date
            Field.IsDeletion, SqlType.Boolean deletionItemDto.IsDeletion
            Field.IsHidden, SqlType.Boolean deletionItemDto.IsHidden
            Field.ToLocation, stringOrNull deletionItemDto.ToLocation
            Field.IsReadyToDeletion, SqlType.Boolean deletionItemDto.IsReadyToDeletion
            Field.OfficeId, SqlType.Guid deletionItemDto.OfficeId
            Field.ChatId, SqlType.Int64 deletionItemDto.ChatId
            Field.DeletionId, SqlType.Guid deletionItemDto.DeletionId ] ]

    transactionManyExn env sqlCommand sqlParamsList

  let updateDeletionItem (env: IDb) (deletionItemDto: DeletionItemDto) =

    let sqlCommand =
      $"UPDATE {DeletionItemDto.TableName}
        SET
          {Field.ItemName} = (@{Field.ItemName}),
          {Field.ItemSerial} = (@{Field.ItemSerial}),
          {Field.ItemMac} = (@{Field.ItemMac}),
          {Field.Count} = (@{Field.Count}),
          {Field.Date} = (@{Field.Date}),
          {Field.IsDeletion} = (@{Field.IsDeletion}),
          {Field.IsHidden} = (@{Field.IsHidden}),
          {Field.ToLocation} = (@{Field.ToLocation}),
          {Field.IsReadyToDeletion} = (@{Field.IsReadyToDeletion}),
          {Field.OfficeId} = (@{Field.OfficeId}),
          {Field.ChatId} = (@{Field.ChatId})
        WHERE {Field.DeletionId} = (@{Field.DeletionId})"

    let sqlParams =
      [ Field.ItemName, SqlType.String deletionItemDto.ItemName
        Field.ItemSerial, stringOrNull deletionItemDto.ItemSerial
        Field.ItemMac, stringOrNull deletionItemDto.ItemMac
        Field.Count, SqlType.Int64 deletionItemDto.Count
        Field.Date, SqlType.Int64 deletionItemDto.Date
        Field.IsDeletion, SqlType.Boolean deletionItemDto.IsDeletion
        Field.IsHidden, SqlType.Boolean deletionItemDto.IsHidden
        Field.ToLocation, stringOrNull deletionItemDto.ToLocation
        Field.IsReadyToDeletion, SqlType.Boolean deletionItemDto.IsReadyToDeletion
        Field.OfficeId, SqlType.Guid deletionItemDto.OfficeId
        Field.ChatId, SqlType.Int64 deletionItemDto.ChatId
        Field.DeletionId, SqlType.Guid deletionItemDto.DeletionId ]

    transactionSingleExn env sqlCommand sqlParams

  let updateTelegramMessage env (messageDto: TelegramMessageDto) =
    let sqlCommand =
      $"UPDATE {TelegramMessageDto.TableName}
        SET {Field.MessageJson} = (@{Field.MessageJson})
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam =
      [ Field.MessageJson, SqlType.String messageDto.MessageJson
        Field.ChatId, SqlType.Int64 messageDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let deleteOffice env (officeDto: OfficeDto) =

    let sqlCommand =
      $"DELETE FROM {OfficeDto.TableName}
        WHERE {Field.OfficeId} = (@{Field.OfficeId})"

    let sqlParam = [ Field.OfficeId, SqlType.Guid officeDto.OfficeId ]

    transactionSingleExn env sqlCommand sqlParam


  let deleteTelegramMessage env (messageDto: TelegramMessageDto) =

    let sqlCommand =
      $"DELETE FROM {TelegramMessageDto.TableName}
        WHERE {Field.ChatId} = (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 messageDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let insertChatId env (chatIdDto: ChatIdDto) =
    let sqlCommand =
      $"INSERT OR IGNORE INTO {ChatIdDto.TableName} ({Field.ChatId}) VALUES (@{Field.ChatId})"

    let sqlParam = [ Field.ChatId, SqlType.Int64 chatIdDto.ChatId ]

    transactionSingleExn env sqlCommand sqlParam

  let officeHiddenBugWorkAround envDb envLog =

    let conn = dbConn envDb

    let sqlCommad = @$"SELECT {FixedField} from {OfficeHiddenBugFixTable}"
    Logger.info envLog $"Get {FixedField} of {OfficeHiddenBugFixTable} with command: {sqlCommad}"

    Db.newCommand sqlCommad conn
    |> Db.querySingle (fun rd -> rd.ReadBoolean FixedField)
    |> function
      | Ok isFixed ->
        if isFixed.IsSome && not isFixed.Value then
          let offices = selectOffices envDb

          match offices with
          | Ok o ->
            for office in o do
              match updateOffice envDb { office with IsHidden = false } with
              | Ok _ ->
                Logger.info
                  envLog
                  $"Success update office {office.OfficeName} for {OfficeHiddenBugFixTable}"
              | Error err ->
                Logger.error
                  envLog
                  $"Error when try update office {office.OfficeName} for {OfficeHiddenBugFixTable}: {err}"

            let sqlUpdateCmd = @$"UPDATE {OfficeHiddenBugFixTable} SET {FixedField} = {true};"

            Logger.info
              envLog
              $"Update {FixedField} of {OfficeHiddenBugFixTable} with command: {sqlUpdateCmd}"

            use tran = conn.TryBeginTransaction()

            Db.newCommand sqlUpdateCmd conn
            |> Db.setTransaction tran
            |> Db.exec
            |> function
              | Ok _ ->
                tran.TryCommit()
                Logger.info envLog $"Success update {FixedField} for {OfficeHiddenBugFixTable}"
              | Error err ->
                tran.TryRollback()

                Logger.error
                  envLog
                  $"Error when update {FixedField} for {OfficeHiddenBugFixTable}: {err}"
          | Error err ->
            Logger.error envLog $"Error when try get offices for {OfficeHiddenBugFixTable}: {err}"
            else
              Logger.debug envLog $"No needed office bug fix, getter value is {isFixed}"
      | Error err ->
        Logger.fatal envLog $"Error when try read {FixedField} of {OfficeHiddenBugFixTable}: {err}"

        DatabaseTryReadOfficeHidenBugFieldTableException(err) |> raise
