namespace WorkTelegram.Infrastructure
    
    module Database =
        
        exception private DatabaseVersionTableNotExistException of string
        
        exception private DatabaseUnhandledVersionException of string
        
        exception private DatabaseVersionTryAddValueException of Donald.DbError
        
        exception private DatabaseVersionHandleVersionException of Donald.DbError
        
        exception private DatabaseTryReadDatabaseVersionTableException of Donald.DbError
        
        val dbConn: env: #AppEnv.IDb -> Microsoft.Data.Sqlite.SqliteConnection
        
        val IDbBuilder:
          conn: Microsoft.Data.Sqlite.SqliteConnection -> AppEnv.IDb
        
        [<Literal>]
        val DbVersionTable: string = "DB_VERSION"
        
        [<Literal>]
        val DbVersionField: string = "VERSION"
        
        val private versionSchema: string
        
        val private schema: string
        
        [<Literal>]
        val ACTUAL_VERISON: int64 = 2L
        
        val private selectVersion:
          envDb: AppEnv.IDb -> envLog: AppEnv.ILog -> int64 option
        
        val private versionHandler:
          envDb: AppEnv.IDb -> envLog: AppEnv.ILog -> unit
        
        val createConnection:
          env: AppEnv.ILog ->
            databaseName: string -> Microsoft.Data.Sqlite.SqliteConnection
        
        val initTables: envDb: AppEnv.IDb -> envLog: AppEnv.ILog -> int
        
        val private stringOrNull: opt: string option -> Donald.SqlType
        
        val private genericSelectMany:
          env: AppEnv.IDb ->
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
          env: AppEnv.IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams ->
            ofDataReader: (System.Data.IDataReader -> 'a) ->
            Result<'a,Core.Types.AppError>
        
        val private transactionSingleExn:
          env: #AppEnv.IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams -> Result<unit,Core.Types.AppError>
        
        val private transactionManyExn:
          env: #AppEnv.IDb ->
            sqlCommand: string ->
            sqlParam: Donald.RawDbParams list ->
            Result<unit,Core.Types.AppError>
        
        val selectTelegramMessages:
          env: AppEnv.IDb ->
            Result<Core.TelegramMessageDto list,Core.Types.AppError>
        
        val selectManagers:
          env: AppEnv.IDb -> Result<Core.ManagerDto list,Core.Types.AppError>
        
        val selectOffices:
          env: AppEnv.IDb -> Result<Core.OfficeDto list,Core.Types.AppError>
        
        val selectEmployers:
          env: AppEnv.IDb -> Result<Core.EmployerDto list,Core.Types.AppError>
        
        val selectDeletionItems:
          env: AppEnv.IDb ->
            Result<Core.DeletionItemDto list,Core.Types.AppError>
        
        val insertTelegramMessage:
          env: AppEnv.IDb ->
            messageDto: Core.TelegramMessageDto ->
            Result<unit,Core.Types.AppError>
        
        val insertManager:
          env: AppEnv.IDb ->
            managerDto: Core.ManagerDto -> Result<unit,Core.Types.AppError>
        
        val insertOffice:
          env: AppEnv.IDb ->
            officeDto: Core.OfficeDto -> Result<unit,Core.Types.AppError>
        
        val insertEmployer:
          env: AppEnv.IDb ->
            employerDto: Core.EmployerDto -> Result<unit,Core.Types.AppError>
        
        val insertDeletionItem:
          env: AppEnv.IDb ->
            deletionItemDto: Core.DeletionItemDto ->
            Result<unit,Core.Types.AppError>
        
        val updateEmployerApprovedByChatId:
          env: AppEnv.IDb ->
            chatIdDto: Core.ChatIdDto ->
            isApproved: bool -> Result<unit,Core.Types.AppError>
        
        val updateOfficeManagerId:
          env: AppEnv.IDb ->
            officeId: int64 ->
            managerId: int64 -> Result<unit,Core.Types.AppError>
        
        val deletionDeletionitemsOfOffice:
          env: AppEnv.IDb ->
            officeId: System.Guid -> Result<unit,Core.Types.AppError>
        
        val hideDeletionItem:
          env: AppEnv.IDb ->
            deletionId: System.Guid -> Result<unit,Core.Types.AppError>
        
        val setReadyToDeletionOfficeItems:
          env: AppEnv.IDb ->
            officeId: System.Guid -> Result<unit,Core.Types.AppError>
        
        val updateTelegramMessage:
          env: AppEnv.IDb ->
            messageDto: Core.TelegramMessageDto ->
            Result<unit,Core.Types.AppError>
        
        val deleteOffice:
          env: AppEnv.IDb ->
            officeId: System.Guid -> Result<unit,Core.Types.AppError>
        
        val deleteTelegramMessage:
          env: AppEnv.IDb ->
            chatIdDto: Core.ChatIdDto -> Result<unit,Core.Types.AppError>
        
        val insertChatId:
          env: AppEnv.IDb ->
            chatIdDto: Core.ChatIdDto -> Result<unit,Core.Types.AppError>

