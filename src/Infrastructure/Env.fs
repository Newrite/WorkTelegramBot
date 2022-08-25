namespace WorkTelegram.Infrastructure

open Microsoft.Data.Sqlite
open System.Collections.Concurrent

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
  type IAppCache<'Command> =
    abstract Agent: Agent<'Command>

  [<Interface>]
  type ICache<'Command> =
    abstract Cache: IAppCache<'Command>

  [<Interface>]
  type IConfigurer<'ElmishCommand> =
    abstract BotConfig: Funogram.Types.BotConfig
    abstract ElmishDict: ConcurrentDictionary<int64, 'ElmishCommand>

  [<Interface>]
  type ICfg<'ElmishCommand> =
    abstract Configurer: IConfigurer<'ElmishCommand>

  type IAppEnv<'CacheCommand, 'ElmishCommand> =
    inherit ILog
    inherit IDb
    inherit ICache<'CacheCommand>
    inherit ICfg<'ElmishCommand>

  let IAppEnvBuilder iLog iDb iCache iCfg =
    { new IAppEnv<_, _> with
        member _.Logger = iLog
        member _.Db = iDb
        member _.Cache = iCache
        member _.Configurer = iCfg }
