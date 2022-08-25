namespace WorkTelegram.Infrastructure
    
    module AppEnv =
        
        type IAppLogger =
            
            abstract Debug: string -> unit
            
            abstract Error: string -> unit
            
            abstract Fatal: string -> unit
            
            abstract Info: string -> unit
            
            abstract Warning: string -> unit
        
        type ILog =
            
            abstract Logger: IAppLogger
        
        type IDatabase =
            
            abstract Conn: Microsoft.Data.Sqlite.SqliteConnection
        
        type IDb =
            
            abstract Db: IDatabase
        
        type IAppCache<'Command> =
            
            abstract Agent: Agent<'Command>
        
        type ICache<'Command> =
            
            abstract Cache: IAppCache<'Command>
        
        type IConfigurer<'ElmishCommand> =
            
            abstract BotConfig: Funogram.Types.BotConfig
            
            abstract
              ElmishDict: System.Collections.Concurrent.ConcurrentDictionary<int64,
                                                                             'ElmishCommand>
        
        type ICfg<'ElmishCommand> =
            
            abstract Configurer: IConfigurer<'ElmishCommand>
        
        [<Interface>]
        type IAppEnv<'CacheCommand,'ElmishCommand> =
            inherit ICfg<'ElmishCommand>
            inherit ICache<'CacheCommand>
            inherit IDb
            inherit ILog
        
        val IAppEnvBuilder:
          iLog: IAppLogger ->
            iDb: IDatabase ->
            iCache: IAppCache<'a> -> iCfg: IConfigurer<'b> -> IAppEnv<'a,'b>

