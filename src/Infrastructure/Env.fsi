namespace WorkTelegram.Infrastructure
    
    [<Interface>]
    type IAppEnv<'ElmishCommand,'CacheCommand> =
        inherit ICfg<'ElmishCommand>
        inherit IRep<'CacheCommand>
        inherit IDb
        inherit ILog
    
    module AppEnv =
        
        val IAppEnvBuilder:
          iLog: ILogger ->
            iDb: IDatabase ->
            iRep: IRepository<'a> -> iCfg: IConfigurer<'b> -> IAppEnv<'b,'a>

