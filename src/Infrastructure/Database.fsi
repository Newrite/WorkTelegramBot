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
        
        val internal selectMessages:
          env: AppEnv.IDb -> Result<Core.MessageDto list,Core.Types.AppError>
        
        val internal selectManagers:
          env: AppEnv.IDb -> Result<Core.ManagerDto list,Core.Types.AppError>
        
        val internal selectOffices:
          env: AppEnv.IDb -> Result<Core.OfficeDto list,Core.Types.AppError>
        
        val internal selectEmployers:
          env: AppEnv.IDb -> Result<Core.EmployerDto list,Core.Types.AppError>
        
        val internal selectDeletionItems:
          env: AppEnv.IDb
            -> Result<Core.DeletionItemDto list,Core.Types.AppError>
        
        val internal selectDeletionItemsByOfficeId:
          env: System.Data.IDbConnection -> officeId: int64
            -> Result<Core.DeletionItemDto list,Core.Types.AppError>
        
        val internal selectMessageByChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<Core.MessageDto,Core.Types.AppError>
        
        val internal selectOfficesByManagerChatId:
          env: System.Data.IDbConnection -> chatIdDto: Core.ChatIdDto
            -> Result<Core.OfficeDto list,Core.Types.AppError>
        
        val internal selectEmployerByChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<Core.EmployerDto,Core.Types.AppError>
        
        val internal selectManagerByChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<Core.ManagerDto,Core.Types.AppError>
        
        val internal selectOfficeById:
          env: AppEnv.IDb -> officeId: int64
            -> Result<Core.OfficeDto,Core.Types.AppError>
        
        val internal selectOfficeByName:
          env: AppEnv.IDb -> officeName: string
            -> Result<Core.OfficeDto,Core.Types.AppError>
        
        val internal selectDeletionItemByTimeTicks:
          env: AppEnv.IDb -> ticks: int64
            -> Result<Core.DeletionItemDto,Core.Types.AppError>
        
        val internal insertMessage:
          env: AppEnv.IDb -> messageDto: Core.MessageDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertManager:
          env: AppEnv.IDb -> managerDto: Core.ManagerDto
            -> Result<unit,Core.Types.AppError>
        
        val internal insertOffice:
          env: AppEnv.IDb -> officeRecord: Core.RecordOffice
            -> Result<unit,Core.Types.AppError>
        
        val internal insertEmployer:
          env: AppEnv.IDb -> employerRecord: Core.RecordEmployer
            -> Result<unit,Core.Types.AppError>
        
        val internal insertDeletionItem:
          env: AppEnv.IDb -> deletionItemRecord: Core.RecordDeletionItem
            -> Result<unit,Core.Types.AppError>
        
        val internal updateEmployerApprovedByChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto -> isApproved: bool
            -> Result<unit,Core.Types.AppError>
        
        val internal setTrueForDeletionFieldOfOfficeItems:
          env: AppEnv.IDb -> officeId: int64 -> Result<unit,Core.Types.AppError>
        
        val internal setTrueForHiddenFieldOfItem:
          env: AppEnv.IDb -> deletionId: int64
            -> Result<unit,Core.Types.AppError>
        
        val internal updateMessage:
          env: AppEnv.IDb -> messageDto: Core.MessageDto
            -> Result<unit,Core.Types.AppError>
        
        val internal deleteOffice:
          env: AppEnv.IDb -> officeId: int64 -> Result<unit,Core.Types.AppError>
        
        val internal deleteMessageJson:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<unit,Core.Types.AppError>
        
        val insertChatId:
          env: AppEnv.IDb -> chatIdDto: Core.ChatIdDto
            -> Result<unit,Core.Types.AppError>
        
        val IDbBuilder:
          conn: Microsoft.Data.Sqlite.SqliteConnection -> AppEnv.IDb

