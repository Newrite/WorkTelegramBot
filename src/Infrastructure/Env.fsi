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
            
            abstract Mailbox: MailboxProcessor<'Command>
        
        type ICache<'Command> =
            
            abstract Cache: IAppCache<'Command>
        
        type IConfigurer =
            
            abstract BotConfig: Funogram.Types.BotConfig
        
        type ICfg =
            
            abstract Configurer: IConfigurer
        
        [<Interface>]
        type IAppEnv<'Command> =
            inherit ICfg
            inherit ICache<'Command>
            inherit IDb
            inherit ILog
        
        val IAppEnvBuilder:
          iLog: IAppLogger -> iDb: IDatabase -> iCache: IAppCache<'a>
          -> iCfg: IConfigurer -> IAppEnv<'a>

