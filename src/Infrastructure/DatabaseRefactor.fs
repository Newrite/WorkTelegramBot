namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

open Donald
open System.Data
open FSharp.UMX
open FSharp.Json
open Microsoft.Data.Sqlite

module DatabaseNew =

  [<Literal>]
  let private Schema =
    @"CREATE TABLE IF NOT EXISTS chat_id_table (
        chat_id INTEGER NOT NULL PRIMARY KEY
      );

      CREATE TABLE IF NOT EXISTS message (
        chat_id INTEGER NOT NULL PRIMARY KEY,
        message_json TEXT NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );
    
      CREATE TABLE IF NOT EXISTS manager (
        chat_id INTEGER NOT NULL PRIMARY KEY,
        firt_name TEXT NOT NULL,
        last_name TEXT NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );
    
      CREATE TABLE IF NOT EXISTS office (
        office_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        office_name TEXT NOT NULL UNIQUE,
        is_hidden BOOL NOT NULL,
        manager_id INTEGER NOT NULL,
        FOREIGN KEY(manager_id) REFERENCES manager(chat_id)
      );

      CREATE TABLE IF NOT EXISTS employer (
	      chat_id INTEGER NOT NULL PRIMARY KEY,
	      first_name TEXT NOT NULL,
	      last_name TEXT NOT NULL,
        is_approved BOOL NOT NULL,
        office_id INTEGER NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id),
	      FOREIGN KEY(office_id) REFERENCES office(office_id)
      );
      
      CREATE TABLE IF NOT EXISTS deletion_items (
        deletion_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        item_name TEXT NOT NULL,
        item_serial TEXT DEFAULT(NULL),
        item_mac TEXT DEFAULT(NULL),
        count INTEGER NOT NULL,
        date INTEGER NOT NULL,
        is_deletion BOOL NOT NULL,
        is_hidden BOOL NOT NULL,
        to_location TEXT DEFAULT(NULL),
        office_id INTEGER NOT NULL,
        chat_id INTEGER NOT NULL,
        FOREIGN KEY(office_id) REFERENCES office(office_id),
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );"

  let createConnection (log: Logging) databaseName =
    log.Debug "Start create connection to database"

    try
      let connectionString = SqliteConnectionStringBuilder()
      connectionString.DataSource <- databaseName
      connectionString.ForeignKeys <- System.Nullable<_>(true)
      let conn = new SqliteConnection(connectionString.ToString())
      conn.Open()
      log.Info $"Success create connection to database = {connectionString.ToString()}"
      conn
    with
    | _ ->
      log.Error $"Failed create connection to database"
      reraise ()

  let initalizationTables env =
    env.Log.Info $"Execute schema sql script"
    use command = new SqliteCommand(Schema, env.DBConn)
    let result = command.ExecuteNonQuery()
    env.Log.Debug $"Schema executed with code: {result}"

  let private unboxOptionOrNotFoundInDatabaseResult opt =
    match opt with
    | Some v -> Ok v
    | None ->
      BusinessError.NotFoundInDatabase
      |> AppError.BusinessError
      |> Error

  let private applyHandlerOrDatabaseError result handler =
    match result with
    | Ok r -> handler r
    | Error err -> err |> AppError.DatabaseError |> Error

  let transactionExn (conn: IDbConnection) (body: IDbConnection -> Result<'a list, DbError>) =

    use tran = conn.TryBeginTransaction()

    let result = body conn

    tran.TryCommit()

    result

  let transactionOptionExn
    (conn: IDbConnection)
    (body: IDbConnection -> Result<'a option, DbError>)
    =

    use tran = conn.TryBeginTransaction()

    let result = body conn

    tran.TryCommit()

    applyHandlerOrDatabaseError result unboxOptionOrNotFoundInDatabaseResult

  let transactionOptionWithCustomHandlerExn
    (conn: IDbConnection)
    (body: IDbConnection -> Result<'a option, DbError>)
    (optionHandler: 'a option -> Result<'a, AppError>)
    =

    use tran = conn.TryBeginTransaction()

    let result = body conn

    tran.TryCommit()

    applyHandlerOrDatabaseError result optionHandler


  let managers conn =
    let sql =
      "
      SELECT  *
      FROM    manager"

    //let param = [ "author_id", SqlType.Int 1 ]


    let unit = conn |> Db.newCommand sql

    fun tran ->
      Db.setTransaction tran unit
      |> Db.query ManagerDto.ofDataReader
  //|> Db.query ManagerDto.ofDataReader

  let inline byId (a: ^a when ^a: (member Id: int64)) = (^a: (member Id: int64) (a))
