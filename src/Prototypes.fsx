open System.Data


#r "nuget: Donald"
#r "nuget: Microsoft.Data.Sqlite"

open Donald
open Microsoft.Data.Sqlite
open System.Data

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
    printfn $"Failed create connection to database"
    reraise ()

let connectionString =
  @"G:\Programming\Languages\F#\WorkTelegramBot\src\App\bin\Debug\net6.0\WorkBotDatabase.sqlite3"

let conn = createConnection connectionString

type Manager =
  { ChatId: int64
    FirstName: string
    LastName: string }

[<RequireQualifiedAccess>]
module Manager =

  let ofDataReader (rd: IDataReader) : Manager =
    { ChatId = rd.ReadInt64 "chat_id"
      FirstName = rd.ReadString "firt_name"
      LastName = rd.ReadString "last_name" }

let managers conn =
  let sql =
    "
    SELECT  *
    FROM    manager"

  //let param = [ "author_id", SqlType.Int 1 ]

  conn
  |> Db.newCommand sql
  //|> Db.setParams
  |> Db.query Manager.ofDataReader

[<RequireQualifiedAccess>]
type BusinessError = | NotFoundInDatabase

[<RequireQualifiedAccess>]
type AppError =
  | DatabaseError of DbError
  | BusinessError of BusinessError

let private unboxOptionOrNotFoundInDatabaseResult opt =
  match opt with
  | Some v -> Ok v
  | None ->
    BusinessError.NotFoundInDatabase
    |> AppError.BusinessError
    |> Error

let genericTransactionExn (conn: IDbConnection) (body: IDbConnection -> Result<'a list, DbError>) =

  use tran = conn.TryBeginTransaction()

  let result = body conn

  tran.TryCommit()

  result

let genericTransactionOptionExn
  (conn: IDbConnection)
  (body: IDbConnection -> Result<'a option, DbError>)
  =

  use tran = conn.TryBeginTransaction()

  let result = body conn

  tran.TryCommit()

  match result with
  | Ok r -> unboxOptionOrNotFoundInDatabaseResult r
  | Error err -> err |> AppError.DatabaseError |> Error

let genericTransactionOptionWithCustomHandlerExn
  (conn: IDbConnection)
  (body: IDbConnection -> Result<'a option, DbError>)
  (optionHandler: 'a option -> Result<'a, AppError>)
  =

  use tran = conn.TryBeginTransaction()

  let result = body conn

  tran.TryCommit()

  match result with
  | Ok r -> optionHandler r
  | Error err -> err |> AppError.DatabaseError |> Error
