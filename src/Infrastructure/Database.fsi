namespace WorkTelegram.Infrastructure
    
    module Database =
        
        val private schema: string
        
        val createConnection:
          env: AppEnv.ILog -> databaseName: string
            -> Microsoft.Data.Sqlite.SqliteConnection
        
        val initTables:
          env: 'a -> unit when 'a :> AppEnv.IDb and 'a :> AppEnv.ILog
        
        val private stringOrNull: opt: string option -> Donald.SqlType
        
        val private genericSelectMany:
          env: AppEnv.IDb -> tableName: string
          -> ofDataReader: (System.Data.IDataReader -> 'a)
            -> Result<'a list,Core.Types.AppError>
        
        val private genericSelectManyWithWhere:
          conn: System.Data.IDbConnection -> sqlCommand: string
          -> sqlParam: Donald.RawDbParams
          -> ofDataReader: (System.Data.IDataReader -> 'a)
            -> Result<'a list,Core.Types.AppError>
        
        val private genericSelectSingle:
          env: AppEnv.IDb -> sqlCommand: string -> sqlParam: Donald.RawDbParams
          -> ofDataReader: (System.Data.IDataReader -> 'a)
            -> Result<'a,Core.Types.AppError>
        
        val private transactionSingleExn:
          env: #AppEnv.IDb -> sqlCommand: string -> sqlParam: Donald.RawDbParams
            -> Result<unit,Core.Types.AppError>
        
        val private transactionManyExn:
          env: #AppEnv.IDb -> sqlCommand: string
          -> sqlParam: Donald.RawDbParams list
            -> Result<unit,Core.Types.AppError>
        
        val internal selectTelegramMessages:
          env: AppEnv.IDb
            -> Result<Core.TelegramMessageDto list,Core.Types.AppError>
        
        val internal selectManagers:
          env: AppEnv.IDb -> Result<Core.ManagerDto list,Core.Types.AppError>
        
        val internal selectOffices:
          env: AppEnv.IDb -> Result<Core.OfficeDto list,Core.Types.AppError>
        
        val internal selectEmployers:
          env: AppEnv.IDb -> Result<Core.EmployerDto list,Core.Types.AppError>
        
        val internal selectDeletionItems:
          env: AppEnv.IDb
            -> Result<Core.DeletionItemDto list,Core.Types.AppError>
        
        val internal insertTelegramMessage:
          env: AppEnv.IDb -> messageDto: Core.TelegramMessageDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertManager:
          env: AppEnv.IDb -> managerDto: Core.ManagerDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertOffice:
          env: AppEnv.IDb -> officeDto: Core.OfficeDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertEmployer:
          env: AppEnv.IDb -> employerDto: Core.EmployerDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertDeletionItem:
          env: AppEnv.IDb -> deletionItemDto: Core.DeletionItemDto
            -> Result<unit,Core.Types.AppError>
        
        val internal updateEmployerApprovedByChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto -> isApproved: bool
            -> Result<unit,Core.Types.AppError>
        
        val internal deletionDeletionitemsOfOffice:
          env: AppEnv.IDb -> officeId: System.Guid
            -> Result<unit,Core.Types.AppError>
        
        val internal hideDeletionItem:
          env: AppEnv.IDb -> deletionId: System.Guid
            -> Result<unit,Core.Types.AppError>
        
        val internal updateTelegramMessage:
          env: AppEnv.IDb -> messageDto: Core.TelegramMessageDto
            -> Result<unit,Core.Types.AppError>
        
        val internal deleteOffice:
          env: AppEnv.IDb -> officeId: System.Guid
            -> Result<unit,Core.Types.AppError>
        
        val internal deleteTelegramMessage:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<unit,Core.Types.AppError>
        
        val insertChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<unit,Core.Types.AppError>
        
        val IDbBuilder:
          conn: Microsoft.Data.Sqlite.SqliteConnection -> AppEnv.IDb

