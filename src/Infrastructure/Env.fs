namespace WorkTelegram.Infrastucture

open WorkTelegram.Core

[<AutoOpen>]
module AppEnv =

  [<Interface>]
  type IAppLogger =
    abstract Error: string -> unit
    abstract Warning: string -> unit
    abstract Fatal: string -> unit
    abstract Debug: string -> unit
    abstract Info: string -> unit
  
  [<Interface>]
  type ILog =
    abstract Logger: IAppLogger
  
  //[<Interface>]
  //type IDatabase =
  //  //abstract InsertEmployer: RecordEmployer -> Result<unit, DbError>
  //  //abstract InsertManager: ManagerDto -> Result<unit, DbError>
  //  //abstract InsertDeletionItem: RecordDeletionItem -> Result<unit, DbError>
  //  //abstract InsertChatId: ChatIdDto -> Result<unit, DbError>
  //  //abstract InsertMessage: MessageDto -> Result<unit, DbError>
  //  //abstract InsertOffice: RecordOffice -> Result<unit, DbError>
  //  abstract Conn: IDbConnection
  //
  //[<Interface>]
  //type IDb =
  //  abstract AppDb: IDatabase
  
  [<Interface>]
  type IAppCache =
    abstract GetOffices: unit -> Office list
    abstract GetDeletionItems: unit -> DeletionItem list
    abstract GetEmployers: unit -> Employer list
    abstract GetManagers: unit -> Manager list
    abstract GetMessages: unit -> Funogram.Telegram.Types.Message list 
    abstract GetOfficesByManagerId: UMX.ChatId -> Office list
    abstract TryGetEmployerByChatId: UMX.ChatId -> Result<Employer, BusinessError>
    abstract TryGetManagerByChatId: UMX.ChatId -> Result<Manager, BusinessError>
    abstract TryGetMessageByChatId: UMX.ChatId -> Result<Funogram.Telegram.Types.Message, BusinessError>
    abstract TryAddOfficeInDb: RecordOffice -> Result<Office, BusinessError>
    abstract TryAddEmployerInDb: RecordEmployer -> Result<Employer, BusinessError>
    abstract TryAddManagerInDb: ManagerDto -> Result<Manager, BusinessError>
    abstract TryAddOrUpdateMessageInDb: MessageDto -> Result<Funogram.Telegram.Types.Message, AppError>
    abstract TryAddDeletionItemInDb: RecordDeletionItem -> Result<DeletionItem, AppError>
    abstract TryUpdateEmployerApprovedInDb: Employer -> bool -> Result<Employer, BusinessError>
    abstract TrySetDeletionOnItemsOfOffice: OfficeId -> Result<unit, BusinessError>
    abstract TryHideDeletionItem: DeletionId -> Result<unit, BusinessError>
    abstract TryDeleteOffice: OfficeId -> Result<unit, BusinessError>
  
  [<Interface>]
  type ICache =
    abstract Cache: IAppCache
  
  [<Interface>]
  type IConfigurer =
    abstract BotConfig: Funogram.Types.BotConfig
  
  [<Interface>]
  type ICfg =
    abstract Configurer: IConfigurer
  
  type IAppEnv =
    inherit ILog
    //inherit IDb
    inherit ICache
    inherit ICfg
