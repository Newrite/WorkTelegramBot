namespace WorkTelegram.Infrastructure

open WorkTelegram.HydraGenerated
open WorkTelegram.Core

open FSharp.UMX

open Microsoft.Data.Sqlite
open FSharp.Json
open SqlHydra.Query
open SqlHydra.Query.SqliteExtensions
open SqlKata
open main


module Database =

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

  let private employers = table<main.employer> |> inSchema (nameof main)
  let private offices = table<main.office> |> inSchema (nameof main)
  let private managers = table<main.manager> |> inSchema (nameof main)

  let private deletionItems =
    table<main.deletion_items>
    |> inSchema (nameof main)

  let private chatIds =
    table<main.chat_id_table>
    |> inSchema (nameof main)

  let private messages = table<main.message> |> inSchema (nameof main)

  let private sharedQueryContext env =
    env.Log.Debug "Call shared query context function"

    Shared
    <| new QueryContext(env.DBConn, Compilers.SqliteCompiler())

  let private createQueryContext env =
    env.Log.Debug "Call create query context function"
    Create(fun () -> new QueryContext(env.DBConn, Compilers.SqliteCompiler()))

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

  let private querySelectOfficeIdByOfficeName (officeName: OfficeName) =

    let officeName = %officeName

    select {
      for office in offices do
        where (office.office_name = officeName)
        select (office.office_id)
    }

  let insertMessageAsync env (message: Funogram.Telegram.Types.Message) =
    task {
      try

        let chatId = message.Chat.Id

        env.Log.Info $"Insert new message from chat id {chatId}"

        let queryContext = sharedQueryContext env

        env.Log.Debug "Start serialize message"

        let serializedMessage = Json.serialize message

        env.Log.Debug "Done serialize message"

        let! inserted =
          insertTask queryContext {
            for m in messages do
              entity
                { message.chat_id = chatId
                  message.message_json = serializedMessage }

              onConflictDoUpdate m.chat_id (m.message_json)
          }

        if inserted > 0 then

          env.Log.Debug "Successfull insert json message"

        else

          env.Log.Debug "No insert json message"

        return ()

      with
      | exn ->

        env.Log.Error $"Exception when try insert message {exn.Message}"

        return ()
    }

  let insertMessage env message =
    let result = insertMessageAsync env message
    result.Result

  let selectMessagesAsync env =
    task {
      try

        env.Log.Info "Try get saved messages"

        let queryContext = sharedQueryContext env

        env.Log.Debug "Select messages json from database"

        let! selected =
          selectTask HydraReader.Read queryContext {
            for m in messages do
              select (m.message_json)
          }

        env.Log.Debug "Success select messages json"

        return
          selected
          |> Seq.map (fun m -> Json.deserialize<Funogram.Telegram.Types.Message> m)
          |> List.ofSeq

      with
      | exn ->

        env.Log.Error
          $"Exception when try get messages json
          with exception message {exn.Message}"

        return []
    }

  let selectMessages env =
    let result = selectMessagesAsync env
    result.Result

  let deleteMessageJsonAsync env (message: Funogram.Telegram.Types.Message) =
    task {
      try

        env.Log.Info "Try delete message json"

        let chatId = message.Chat.Id

        let queryContext = sharedQueryContext env

        let! deleted =
          deleteTask queryContext {
            for m in messages do
              where (m.chat_id = chatId)
          }

        env.Log.Debug $"Success delete {deleted} messsages json"

        return ()

      with
      | exn ->

        env.Log.Error
          $"Exception when try delete message json
          with exception message {exn.Message}"

        return ()
    }

  let deleteMessageJson env message =
    let result = deleteMessageJsonAsync env message
    result.Result

  let insertChatIdAsync env (chatId: UMX.ChatId) =
    task {
      try

        env.Log.Info $"Insert new chat id in chat id table {chatId}"

        let queryContext = sharedQueryContext env

        let! inserted =
          insertTask queryContext {
            for ci in chatIds do
              entity { chat_id_table.chat_id = %chatId }
              onConflictDoNothing ci.chat_id
          }

        if inserted > 0 then
          env.Log.Debug $"Successfull insert chat id in chat id table {chatId}"
          return chatId |> Ok
        else
          env.Log.Debug $"Chat id {chatId} already exist in caht id table"

          return
            DatabaseError.ChatIdAlreadyExistInDatabase chatId
            |> Error

      with
      | :? Microsoft.Data.Sqlite.SqliteException as exn ->
        env.Log.Debug
          $"Exception when tryed insert chat id {chatId} in chat id table, exn message = {exn.Message}"

        return DatabaseError.SQLiteException exn |> Error
    }

  let insertChatId env chatId =
    let result = insertChatIdAsync env chatId
    result.Result

  /// Insert manager in database and update managers list in cache
  let insertManagerAsync env (managerToAdd: RecordedManager) =

    let insertTaskQ qContext =

      insertTask qContext {
        for m in managers do
          entity
            { manager.chat_id = %managerToAdd.ChatId
              manager.firt_name = %managerToAdd.FirstName
              manager.last_name = %managerToAdd.LastName }

          onConflictDoNothing m.chat_id
      }

    task {
      env.Log.Info "Insert new manager in database"
      let queryContext = sharedQueryContext env

      try
        let! inserted = insertTaskQ queryContext

        if inserted > 0 then
          env.Log.Debug $"Success insert new manager, chat id = {managerToAdd.ChatId}"

          CacheCommand.AddManager managerToAdd
          |> env.CacheActor.Post

          return Ok managerToAdd
        else
          env.Log.Error
            $"Failed insert new manager with chat id = {managerToAdd.ChatId}
              manager with this chat id already exist"

          return Error "Manager with this chat id already exists"
      with
      | exn ->
        env.Log.Error
          $"Failed insert new manager to database, exn message = {exn.Message}
            with manager chat id = {managerToAdd.ChatId}"

        return Error exn.Message
    }

  let insertManager env managerToAdd =
    let result = insertManagerAsync env managerToAdd
    result.Result

  let selectManagerByChatIdAsync env (chatId: UMX.ChatId) =
    task {
      let chatId = %chatId
      env.Log.Info "Select manager by chat id"
      let queryContext = sharedQueryContext env

      let! selected =
        selectTask HydraReader.Read queryContext {
          for m in managers do
            where (m.chat_id = chatId)
            yield m
        }

      match Seq.tryHead selected with
      | Some m ->
        env.Log.Debug $"Selected manager with chat id = {chatId}"

        return
          Some
            { ChatId = %m.chat_id
              FirstName = %m.firt_name
              LastName = %m.last_name }
      | None ->
        env.Log.Warning $"Manager with chat id = {chatId} not found"
        return None
    }

  let selectManagerByChatId env chatId =
    let result = selectManagerByChatIdAsync env chatId
    result.Result

  let insertEmployerAsync env (employerToAdd: RecordedEmployer) isApproved =

    let insertTaskQ qContext office =
      insertTask qContext {
        for e in employers do
          entity
            { employer.chat_id = %employerToAdd.ChatId
              employer.first_name = %employerToAdd.FirstName
              employer.last_name = %employerToAdd.LastName
              employer.is_approved = isApproved
              employer.office_id = office.office_id }

          onConflictDoNothing e.chat_id
      }

    task {
      env.Log.Info "Insert new employer in database"
      let queryContext = sharedQueryContext env
      let managerChatId = %employerToAdd.Office.Manager.ChatId

      let! selected =
        selectTask HydraReader.Read queryContext {
          for o in offices do
            where (o.manager_id = managerChatId)
            yield o
        }

      match Seq.tryHead selected with
      | Some o ->
        env.Log.Debug "Succes selected office for employer"

        try
          let! inserted = insertTaskQ queryContext o

          if inserted > 0 then
            env.Log.Debug
              $"Success insert employer in database with chat id = {employerToAdd.ChatId}"

            CacheCommand.AddEmployer employerToAdd
            |> env.CacheActor.Post

            return Ok employerToAdd
          else
            env.Log.Error
              $"Failed insert employer in database with caht id = {employerToAdd.ChatId}
                employer with this chat id already exist"

            return Error "Employer with this chat id already exist"
        with
        | exn ->
          env.Log.Error
            $"Failed insert employer with exn message = {exn.Message}
              employer chat id = {employerToAdd.ChatId}"

          return Error exn.Message
      | None ->
        env.Log.Error
          $"Failed insert employer with chat id = {employerToAdd.ChatId}
          can't find office with manager chat id = {managerChatId}"

        return Error "Can't find office with manager chat id"
    }

  let insertEmployer env employerToAdd isApproved =
    let result = insertEmployerAsync env employerToAdd isApproved
    result.Result

  let selectEmployerByChatIdAsync env (chatId: UMX.ChatId) =
    task {
      let chatId = %chatId
      env.Log.Info "Select employer by chat id"
      let queryContext = sharedQueryContext env

      let! selected =
        selectTask HydraReader.Read queryContext {
          for e in employers do
            where (e.chat_id = chatId)
            join o in offices on (e.office_id = o.office_id)
            join m in managers on (o.manager_id = m.chat_id)
            yield (e, o, m)
        }

      match Seq.tryHead selected with
      | Some (e, o, m) ->
        env.Log.Debug
          $"Success selected office, manager and employer database records
            for employer with chat id = {chatId}"

        return
          Some
            { FirstName = %e.first_name
              LastName = %e.last_name
              Office =
                { OfficeName = %o.office_name
                  Manager =
                    { ChatId = %m.chat_id
                      FirstName = %m.firt_name
                      LastName = %m.last_name } }
              ChatId = %e.chat_id }
      | None ->
        env.Log.Warning
          $"Failed select employer by chat id = {chatId}
          office, manager or employer database records not found"

        return None
    }

  let selectEmployerByChatId env chatId =
    let result = selectEmployerByChatIdAsync env chatId
    result.Result

  /// Insert office in database and update office list in cache
  let insertOfficeAsync env (office: RecordedOffice) =

    let chatId = %office.Manager.ChatId
    let officeName = %office.OfficeName

    let managerAsEmployer =
      { FirstName = office.Manager.FirstName
        LastName = office.Manager.LastName
        Office = office
        ChatId = office.Manager.ChatId }

    let insertTaskQ qContext =

      insertTask qContext {
        for o in offices do
          entity
            { office.office_id = 0L
              office.office_name = officeName
              office.manager_id = chatId
              office.is_hidden = false }

          onConflictDoNothing o.office_name
          excludeColumn o.office_id
      }

    task {

      env.Log.Info "Insert new office in database"

      let queryContext = sharedQueryContext env

      try

        let! inserted = insertTaskQ queryContext

        if inserted > 0 then

          env.Log.Debug $"Success insert new office with manager chat id = {chatId}, update Cache"

          office
          |> CacheCommand.AddOffice
          |> env.CacheActor.Post

          return Ok office

        else

          env.Log.Error
            $"Failed insert new office with manager chat id = {chatId}
              name already exist, office name = {officeName}"

          return Error "Conflict name"

      with
      | exn ->

        env.Log.Error
          $"Failed insert new office with exn message {exn.Message}
            manager chat id = {chatId} office name = {officeName}"

        return Error exn.Message
    }

  /// Insert office in database and update office list in cache
  let insertOffice env office =
    let result = insertOfficeAsync env office
    result.Result

  let selectOfficesByManagerChatIdAsync env (chatId: UMX.ChatId) =
    task {
      let chatId = %chatId
      env.Log.Info "Select office by manager chat id"
      let queryContext = sharedQueryContext env

      let! selected =
        selectTask HydraReader.Read queryContext {
          for o in offices do
            where (o.manager_id = chatId)
            join m in managers on (o.manager_id = m.chat_id)
            yield (o, m)
        }

      return
        selected
        |> List.ofSeq
        |> List.map (fun (o, m) ->
          { OfficeName = %o.office_name
            Manager =
              { ChatId = %m.chat_id
                FirstName = %m.firt_name
                LastName = %m.last_name } })
    }

  let selectOfficesByManagerChatId env chatId =
    let result = selectOfficesByManagerChatIdAsync env chatId
    result.Result

  //let selectOfficeByManagerChatIdAsync env chatId =
  //  task {
  //    let! officesAndManager = selectOfficesByManagerChatIdAsync env chatId
  //    match List.tryHead officesAndManager with
  //    | Some o ->
  //      env.Log.Debug $"Selected office by manager chat id = {chatId}"
  //      return
  //       Some o
  //    | None   ->
  //      env.Log.Warning $"Failed select office with manager chat id = {chatId}
  //        office not found"
  //      return None
  //  }
  //
  //let selectOfficeByManagerChatId env chatId =
  //  let result = selectOfficeByManagerChatIdAsync env chatId
  //  result.Result

  let insertDeletionItemAsync env (itemToAdd: RecordedDeletionItem) =

    let insertTaskQ qContext office =

      let mac = Option.map (fun m -> %m) itemToAdd.Item.MacAddress
      let serial = Option.map (fun s -> %s) itemToAdd.Item.Serial
      let location = Option.map (fun l -> %l) itemToAdd.Location
      let date = let t = itemToAdd.Time in t.Ticks
      let count = let v = itemToAdd.Count in int64 v.GetValue
      let name = %itemToAdd.Item.Name
      let chatId = %itemToAdd.Employer.ChatId
      let officeId = office.office_id

      insertTask qContext {
        for di in deletionItems do
          entity
            { deletion_items.deletion_id = 0L
              deletion_items.item_name = name
              deletion_items.item_serial = serial
              deletion_items.item_mac = mac
              deletion_items.count = count
              deletion_items.date = date
              deletion_items.is_deletion = false
              deletion_items.is_hidden = false
              deletion_items.to_location = location
              deletion_items.office_id = officeId
              deletion_items.chat_id = chatId }

          excludeColumn di.deletion_id
      }

    task {
      let queryContext = sharedQueryContext env
      let chatId = %itemToAdd.Employer.Office.Manager.ChatId

      let! selected =
        selectTask HydraReader.Read queryContext {
          for o in offices do
            where (o.manager_id = chatId)
            yield o
        }

      match Seq.tryHead selected with
      | Some o ->
        let! inserted = insertTaskQ queryContext o
        return Some inserted
      | None -> return None
    }

  let insertDeletionItem env (itemToAdd: RecordedDeletionItem) =
    let result = insertDeletionItemAsync env itemToAdd
    result.Result

  let internal initializeCacheAsync env =
    task {

      let queryContext = sharedQueryContext env

      let! offices =
        selectTask HydraReader.Read queryContext {
          for o in offices do
            yield o
        }

      env.Log.Debug "Init Cache: get database offices"

      let! managers =
        selectTask HydraReader.Read queryContext {
          for m in managers do
            yield m
        }

      env.Log.Debug "Init Cache: get database managers"

      let! employers =
        selectTask HydraReader.Read queryContext {
          for e in employers do
            yield e
        }

      env.Log.Debug "Init Cache: get database employers"

      let managersCache =
        managers
        |> Seq.map (fun m ->
          { ChatId = %m.chat_id
            FirstName = %m.firt_name
            LastName = %m.last_name })
        |> List.ofSeq

      env.Log.Debug "Init Cache: get transformed managers"

      let officesCache =
        offices
        |> Seq.map (fun o ->
          { OfficeName = %o.office_name
            Manager = List.find (fun m -> m.ChatId = %o.manager_id) managersCache })
        |> List.ofSeq

      env.Log.Debug "Init Cache: get transformed offices"

      let employersCache =
        employers
        |> Seq.map (fun e ->
          let office =
            let o =
              offices
              |> Seq.find (fun o -> o.office_id = e.office_id)

            officesCache
            |> List.find (fun oc -> oc.Manager.ChatId = %o.manager_id)

          { FirstName = %e.first_name
            LastName = %e.last_name
            ChatId = %e.chat_id
            Office = office })
        |> List.ofSeq

      env.Log.Debug "Init Cache: get transformed employers"

      return
        { Employers = employersCache
          Offices = officesCache
          Managers = managersCache }
    }

  let internal initializeCache env =
    let result = initializeCacheAsync env
    result.Result

  let isApprovedAsync env (employer: RecordedEmployer) =
    task {

      let queryContext = sharedQueryContext env

      let chatId = %employer.ChatId

      let! selected =
        selectTask HydraReader.Read queryContext {
          for e in employers do
            where (e.chat_id = chatId)
            yield e
        }

      return selected |> Seq.exists ^ fun e -> e.is_approved
    }

  let isApproved env (employer: RecordedEmployer) =
    let result = isApprovedAsync env employer
    result.Result

  let selectDeletionItemsAsync env (employer: RecordedEmployer) =
    task {

      let queryContext = sharedQueryContext env

      let chatId = %employer.ChatId
      let officeName = %employer.Office.OfficeName

      let! selected =

        let officeId =
          select {
            for o in offices do
              where (o.office_name = officeName)
              select (o.office_id)
          }

        selectTask HydraReader.Read queryContext {
          for di in deletionItems do
            where (
              di.chat_id = chatId
              && not di.is_hidden
              && not di.is_deletion
              && di.office_id = subqueryOne officeId
            )

            yield di
        }

      return
        selected
        |> List.ofSeq
        |> List.filter (fun di ->
          let date = System.DateTime.FromBinary(di.date)

          let total =
            let since = (System.DateTime.Now - date)
            since.TotalHours

          total < 24.)
        |> List.map (fun di ->
          let name: ItemName = %di.item_name
          let macaddress: MacAddress option = Option.map (fun m -> %m) di.item_mac
          let serial: Serial option = Option.map (fun s -> %s) di.item_serial
          let location: Location option = Option.map (fun l -> %l) di.to_location

          let count: PositiveInt =
            let a =
              match di.count |> uint |> PositiveInt.create with
              | Ok v -> Some v
              | Error _ ->

              env.Log.Error
                $"Negative or zero count from deletion item record: DI record form bd = {di}"

              None

            a.Value

          let item =
            { Item = Item.create name serial macaddress
              Count = count
              Time = System.DateTime.FromBinary(di.date)
              Location = location
              Employer = employer }

          (item, di.deletion_id))

    }

  let selectDeletionItems env (employer: RecordedEmployer) =
    let result = selectDeletionItemsAsync env employer
    result.Result

  let updateIsApprovedEmployerAsync env isApproved (employer: RecordedEmployer) =
    task {

      let queryContext = sharedQueryContext env

      let chatId = %employer.ChatId

      env.Log.Info $"Try update approve to {isApproved} for employer with chat id {chatId}"

      try

        let! updated =
          updateTask queryContext {
            for e in employers do
              set e.is_approved isApproved
              where (e.chat_id = chatId)
          }

        if updated > 0 then
          env.Log.Debug $"Success set {isApproved} in approved field employer in chat id {chatId}"
          return employer |> Ok
        else
          env.Log.Warning $"Approved field of employer is not updated to {isApproved}"

          return
            employer
            |> DatabaseError.CantUpdateEmployerApproved
            |> Error
      with
      | :? Microsoft.Data.Sqlite.SqliteException as qe ->
        env.Log.Error
          $"Exception when try update approved field of employer, chat id {chatId} exn message {qe.Message}"

        return qe |> DatabaseError.SQLiteException |> Error

    }

  let updateIsApprovedEmployer env isApproved employer =
    let result = updateIsApprovedEmployerAsync env isApproved employer
    result.Result

  let setIsHiddenTrueForItemAsync env itemId =
    task {

      let queryContext = sharedQueryContext env

      env.Log.Info $"Try set is_hidden item to true for item with id {itemId}"

      try

        let! updated =
          updateTask queryContext {
            for di in deletionItems do
              set di.is_hidden true
              where (di.deletion_id = itemId)
          }

        if updated > 0 then
          env.Log.Debug $"Success set is_hidden item to true for item with id {itemId}"
          return itemId |> Ok
        else
          env.Log.Warning $"Failed set is_hidden item to true for item with id {itemId}"

          return
            itemId
            |> DatabaseError.CantDeleteRecordedItem
            |> Error
      with
      | :? Microsoft.Data.Sqlite.SqliteException as qe ->
        env.Log.Error
          $"Exception when try set is_hidden item to true for item with id {itemId} exn message {qe.Message}"

        return qe |> DatabaseError.SQLiteException |> Error

    }

  let setIsHiddenTrueForItem env itemId =
    let result = setIsHiddenTrueForItemAsync env itemId
    result.Result

  let setIsDeletionTrueForAllItemsInOfficeAsync env (office: RecordedOffice) =
    task {

      let officeName: string = %office.OfficeName

      let queryContext = sharedQueryContext env

      env.Log.Info $"Try set is_deletion true for items in office with name {officeName}"

      try

        let officeId =
          select {
            for o in offices do
              where (o.office_name = officeName)
              select (o.office_id)
          }

        let! updated =
          updateTask queryContext {
            for di in deletionItems do
              set di.is_deletion true

              where (
                not di.is_deletion
                && not di.is_hidden
                && di.office_id = subqueryOne officeId
              )
          }

        env.Log.Debug $"Deletions {updated} items in office with name {officeName}"
        return updated |> Ok

      with
      | :? Microsoft.Data.Sqlite.SqliteException as qe ->

        env.Log.Error
          $"Exception when try deltions item in office with name {officeName} exn message {qe.Message}"

        return DatabaseError.SQLiteException qe |> Error

    }

  let setIsDeletionTrueForAllItemsInOffice env office =
    let result = setIsDeletionTrueForAllItemsInOfficeAsync env office
    result.Result

  let tryDeleteOfficeByOfficeNameAndUpdateCacheAsync env (office: RecordedOffice) =
    task {

      let queryContext = sharedQueryContext env

      let officeName: string = %office.OfficeName

      env.Log.Info $"Try delete office with name {officeName}"

      try

        let! deleted =
          deleteTask queryContext {
            for o in offices do
              where (o.office_name = officeName)
          }

        if deleted > 0 then
          env.Log.Debug $"Office deleted with name {officeName}"

          CacheCommand.DeleteOffice office
          |> env.CacheActor.Post

          return office |> Ok
        else
          env.Log.Debug $"Office with name {officeName} not deleted"
          return DatabaseError.CantDeleteOffice office |> Error

      with
      | :? Microsoft.Data.Sqlite.SqliteException as qe ->

        env.Log.Warning
          $"SQLite Exception when try delete office with name {officeName} exn message {qe.Message}"

        return DatabaseError.SQLiteException qe |> Error

      | exn ->

        env.Log.Warning
          $"Exception when try delete office with name {officeName} exn message {exn.Message}"

        return DatabaseError.UnknownException exn |> Error

    }

  let tryDeleteOfficeByOfficeNameAndUpdateCache env office =
    let result = tryDeleteOfficeByOfficeNameAndUpdateCacheAsync env office
    result.Result

  let selectAllItemsByOfficeAsync env (office: RecordedOffice) =
    task {

      let queryContext = sharedQueryContext env

      env.Log.Info $"Try get all actual items from office with name {office.OfficeName}"

      try

        let queryOfficeId = querySelectOfficeIdByOfficeName office.OfficeName

        let! selected =
          selectTask HydraReader.Read queryContext {
            for di in deletionItems do
              where (di.office_id = subqueryOne queryOfficeId)
              yield (di)
          }

        env.Log.Debug $"Get actual items from office with name {office.OfficeName}"

        return
          selected
          |> List.ofSeq
          |> List.map (fun di ->
            let name: ItemName = %di.item_name
            let macaddress: MacAddress option = Option.map (fun m -> %m) di.item_mac
            let serial: Serial option = Option.map (fun s -> %s) di.item_serial
            let location: Location option = Option.map (fun l -> %l) di.to_location

            let count: PositiveInt =
              let a =
                match di.count |> uint |> PositiveInt.create with
                | Ok v -> Some v
                | Error _ ->

                env.Log.Error
                  $"Negative or zero count from deletion item record: DI record form bd = {di}"

                None

              a.Value

            let item = Item.create name serial macaddress

            let employer =
              let e = selectEmployerByChatId env %di.chat_id
              let m = selectManagerByChatId env %di.chat_id

              if e.IsSome then
                let e = e.Value

                { FirstName = e.FirstName
                  LastName = e.LastName
                  ChatId = e.ChatId
                  Office = office }
              else

              let m = m.Value

              { FirstName = m.FirstName
                LastName = m.LastName
                ChatId = m.ChatId
                Office = office }

            let recordedItem =
              { Item = item
                Count = count
                Time = System.DateTime.FromBinary(di.date)
                Location = location
                Employer = employer }

            {| RecordedItem = recordedItem
               IsHidden = di.is_hidden
               IsDeletion = di.is_deletion
               Id = di.deletion_id |})
          |> Ok

      with
      | :? SqliteException as qe ->

        env.Log.Warning
          $"SQLite Exception when try get all actual items from
              office with name {office.OfficeName} exn message {qe.Message}"

        return DatabaseError.SQLiteException qe |> Error

      | exn ->

        env.Log.Warning
          $"Exception when try get all actual items from
              office with name {office.OfficeName} exn message {exn.Message}"

        return DatabaseError.UnknownException exn |> Error

    }

  let selectAllItemsByOffice env office =
    let result = selectAllItemsByOfficeAsync env office
    result.Result
