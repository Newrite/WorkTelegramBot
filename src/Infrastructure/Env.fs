namespace WorkTelegram.Infrastucture

open WorkTelegram.Core
open Donald
open System.Data

[<Interface>]
type ILogger =
  abstract member Error: string -> unit
  abstract member Warning: string -> unit
  abstract member Fatal: string -> unit
  abstract member Debut: string -> unit
  abstract member Info: string -> unit

[<Interface>]
type ILog =
  abstract Logger: ILogger

[<Interface>]
type IDatabase =
  //abstract member InsertEmployer: RecordEmployer -> Result<unit, DbError>
  //abstract member InsertManager: ManagerDto -> Result<unit, DbError>
  //abstract member InsertDeletionItem: RecordDeletionItem -> Result<unit, DbError>
  //abstract member InsertChatId: ChatIdDto -> Result<unit, DbError>
  //abstract member InsertMessage: MessageDto -> Result<unit, DbError>
  //abstract member InsertOffice: RecordOffice -> Result<unit, DbError>
  abstract Conn: IDbConnection

[<Interface>]
type IDb =
  abstract AppDb: IDatabase

[<Interface>]
type ICacheInterface =
  abstract member tryGetEmployerByChatId: UMX.ChatId -> Result<Employer, BusinessError>
  abstract member tryGetManagerByChatId: UMX.ChatId -> Result<Manager, BusinessError>
  abstract member tryGetOfficeByManagerId: UMX.ChatId -> Result<Office, BusinessError>
  abstract member getOffices: unit -> Office list
  abstract member getDeltionItems: unit -> DeletionItem list
  abstract member tryAddOfficeInDb: RecordOffice -> Result<Office, BusinessError>
  abstract member tryAddEmployerInDb: RecordEmployer -> Result<Employer, BusinessError>

[<Interface>]
type ICache =
  abstract Cache: ICacheInterface

[<Interface>]
type IConfigurer =
  abstract BotConfig: Funogram.Types.BotConfig

[<Interface>]
type ICfg =
  abstract Configurer: IConfigurer

type IAppEnv =
  inherit ILog
  inherit IDb
  inherit ICache
  inherit ICfg
