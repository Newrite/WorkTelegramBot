namespace WorkTelegram.Infrastructure
    
    [<Interface>]
    type IAppEnv<'ElmishCommand,'CacheCommand> =
        inherit IEventBus
        inherit ICfg<'ElmishCommand>
        inherit IRep<'CacheCommand>
        inherit IDb
        inherit ILog
    
    [<AutoOpen>]
    module AppEnv =
        
        val IAppEnvBuilder:
          iLog: ILogger ->
            iDb: IDatabase ->
            iRep: IRepository<'a> ->
            iCfg: IConfigurer<'b> -> iBus: IEvent -> IAppEnv<'b,'a>

