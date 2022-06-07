namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

open Microsoft.Data.Sqlite

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

  [<Interface>]
  type IDatabase =
    abstract Conn: SqliteConnection

  [<Interface>]
  type IDb =
    abstract Db: IDatabase

  [<Interface>]
  type IAppCache<'a> =
    abstract Mailbox: MailboxProcessor<'a>

  [<Interface>]
  type ICache<'a> =
    abstract Cache: IAppCache<'a>

  [<Interface>]
  type IConfigurer =
    abstract BotConfig: Funogram.Types.BotConfig

  [<Interface>]
  type ICfg =
    abstract Configurer: IConfigurer

  type IAppEnv<'a> =
    inherit ILog
    inherit IDb
    inherit ICache<'a>
    inherit ICfg

  let IAppEnvBuilder iLog iDb iCache iCfg =
    { new IAppEnv<_> with
        member _.Logger = iLog
        member _.Db = iDb
        member _.Cache = iCache
        member _.Configurer = iCfg }
