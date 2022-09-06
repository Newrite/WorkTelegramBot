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
        
        val dbConn: env: #IDb -> Microsoft.Data.Sqlite.SqliteConnection
        
        val IDbBuilder: conn: Microsoft.Data.Sqlite.SqliteConnection -> IDb
        
        [<Literal>]
        val DbVersionTable: string = "DB_VERSION"
        
        [<Literal>]
        val DbVersionField: string = "VERSION"
        
        val private versionSchema: string
        
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
            Result<'a list,Core.Types.AppError>
        
        val private genericSelectManyWithWhere:
          conn: System.Data.IDbConnection ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a list,Core.Types.AppError>
        
        val private genericSelectSingle:
          env: IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a,Core.Types.AppError>
        
        val private transactionSingleExn:
          env: #IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams -> Result<unit,Core.Types.AppError>
        
        val private transactionManyExn:
          env: #IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams list ->
            Result<unit,Core.Types.AppError>
        
        val selectTelegramMessages:

          env: IDb -> Result<Core.TelegramMessageDto list,Core.Types.AppError>
        
        val selectManagers:
          env: IDb -> Result<Core.ManagerDto list,Core.Types.AppError>
        
        val selectOffices:
          env: IDb -> Result<Core.OfficeDto list,Core.Types.AppError>
        
        val selectEmployers:
          env: IDb -> Result<Core.EmployerDto list,Core.Types.AppError>
        
        val selectDeletionItems:
          env: IDb -> Result<Core.DeletionItemDto list,Core.Types.AppError>
        
        val insertTelegramMessage:
          env: IDb ->
            messageDto: Core.TelegramMessageDto ->
            Result<unit,Core.Types.AppError>
        
        val insertManager:
          env: IDb ->
            managerDto: Core.ManagerDto -> Result<unit,Core.Types.AppError>
        
        val insertOffice:
          env: IDb ->
            officeDto: Core.OfficeDto -> Result<unit,Core.Types.AppError>
        
        val insertEmployer:
          env: IDb ->
            employerDto: Core.EmployerDto -> Result<unit,Core.Types.AppError>
        
        val insertDeletionItem:
          env: IDb ->
            deletionItemDto: Core.DeletionItemDto ->
            Result<unit,Core.Types.AppError>
        
        val updateEmployer:
          env: IDb ->
            employerDto: Core.EmployerDto -> Result<unit,Core.Types.AppError>
        
        val updateOffice:
          env: IDb ->
            officeDto: Core.OfficeDto -> Result<unit,Core.Types.AppError>
        
        val updateDeletionItems:
          env: IDb ->
            deletionItemsDtos: Core.DeletionItemDto list ->
            Result<unit,Core.Types.AppError>
        
        val updateDeletionItem:
          env: IDb ->
            deletionItemDto: Core.DeletionItemDto ->
            Result<unit,Core.Types.AppError>
        
        val updateTelegramMessage:
          env: IDb ->
            messageDto: Core.TelegramMessageDto ->
            Result<unit,Core.Types.AppError>
        
        val deleteOffice:
          env: IDb ->
            officeDto: Core.OfficeDto -> Result<unit,Core.Types.AppError>
        
        val deleteTelegramMessage:
          env: IDb ->
            messageDto: Core.TelegramMessageDto ->
            Result<unit,Core.Types.AppError>
        
        val insertChatId:
          env: IDb ->
            chatIdDto: Core.ChatIdDto -> Result<unit,Core.Types.AppError>

