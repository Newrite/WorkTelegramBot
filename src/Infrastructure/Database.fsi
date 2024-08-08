namespace WorkTelegram.Infrastructure
    
    type IDatabase =
        
        abstract Conn: Microsoft.Data.Sqlite.SqliteConnection
    
    type IDb =
        
        abstract Db: IDatabase
    
    module Database =
        
        exception private DatabaseVersionTableNotExistException of string
        
        exception private DatabaseUnhandledVersionException of string
        
        exception private DatabaseVersionTryAddValueException of Donald.DbError
        
        exception private DatabaseVersionHandleVersionException of Donald.DbError
        
        exception private DatabaseTryReadDatabaseVersionTableException of Donald.DbError
        
        exception private DatabaseTryReadOfficeHidenBugFieldTableException of Donald.DbError
        
        val dbConn: env: #IDb -> Microsoft.Data.Sqlite.SqliteConnection
        
        val IDbBuilder: conn: Microsoft.Data.Sqlite.SqliteConnection -> IDb
        
        [<Literal>]
        val DbVersionTable: string = "DB_VERSION"
        
        [<Literal>]
        val DbVersionField: string = "VERSION"
        
        [<Literal>]
        val OfficeHiddenBugFixTable: string = "OFFICE_HIDDEN_BUG_FIX"
        
        [<Literal>]
        val FixedField: string = "FIXED"
        
        val private versionSchema: string
        
        val private officeHiddenBugFixSchema: string
        
        val private schema: string
        
        [<Struct>]
        type DatabaseVersions =
            | FirstVersion = 1
            | ActualVersion = 2
        
        val private selectVersion:
          envDb: IDb -> envLog: ILog -> DatabaseVersions option
        
        val private versionHandler: envDb: IDb -> envLog: ILog -> unit
        
        val createConnection:
          env: ILog ->
            databaseName: string -> Microsoft.Data.Sqlite.SqliteConnection
        
        val initTables: envDb: IDb -> envLog: ILog -> int
        
        val private stringOrNull: opt: string option -> Donald.SqlType
        
        val private genericSelectMany:
          env: IDb ->
            tableName: string ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a list,WorkTelegram.Core.Types.AppError>
        
        val private genericSelectManyWithWhere:
          conn: System.Data.IDbConnection ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a list,WorkTelegram.Core.Types.AppError>
        
        val private genericSelectSingle:
          env: IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a,WorkTelegram.Core.Types.AppError>
        
        val private transactionSingleExn:
          env: #IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val private transactionManyExn:
          env: #IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams list ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val selectTelegramMessages:
          env: IDb ->
            Result<WorkTelegram.Core.TelegramMessageDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val selectManagers:
          env: IDb ->
            Result<WorkTelegram.Core.ManagerDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val selectOffices:
          env: IDb ->
            Result<WorkTelegram.Core.OfficeDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val selectEmployers:
          env: IDb ->
            Result<WorkTelegram.Core.EmployerDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val selectDeletionItems:
          env: IDb ->
            Result<WorkTelegram.Core.DeletionItemDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val selectChatIds:
          env: IDb ->
            Result<WorkTelegram.Core.ChatIdDto list,
                   WorkTelegram.Core.Types.AppError>
        
        val insertTelegramMessage:
          env: IDb ->
            messageDto: WorkTelegram.Core.TelegramMessageDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val insertManager:
          env: IDb ->
            managerDto: WorkTelegram.Core.ManagerDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val insertOffice:
          env: IDb ->
            officeDto: WorkTelegram.Core.OfficeDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val insertEmployer:
          env: IDb ->
            employerDto: WorkTelegram.Core.EmployerDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val insertDeletionItem:
          env: IDb ->
            deletionItemDto: WorkTelegram.Core.DeletionItemDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val updateEmployer:
          env: IDb ->
            employerDto: WorkTelegram.Core.EmployerDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val updateOffice:
          env: IDb ->
            officeDto: WorkTelegram.Core.OfficeDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val updateDeletionItems:
          env: IDb ->
            deletionItemsDtos: WorkTelegram.Core.DeletionItemDto list ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val updateDeletionItem:
          env: IDb ->
            deletionItemDto: WorkTelegram.Core.DeletionItemDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val updateTelegramMessage:
          env: IDb ->
            messageDto: WorkTelegram.Core.TelegramMessageDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val deleteOffice:
          env: IDb ->
            officeDto: WorkTelegram.Core.OfficeDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val deleteTelegramMessage:
          env: IDb ->
            messageDto: WorkTelegram.Core.TelegramMessageDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val insertChatId:
          env: IDb ->
            chatIdDto: WorkTelegram.Core.ChatIdDto ->
            Result<unit,WorkTelegram.Core.Types.AppError>
        
        val officeHiddenBugWorkAround: envDb: IDb -> envLog: ILog -> unit

