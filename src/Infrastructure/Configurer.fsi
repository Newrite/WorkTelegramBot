namespace WorkTelegram.Infrastructure
    
    module Configurer =
        
        val botConfig: env: #AppEnv.ICfg -> Funogram.Types.BotConfig
        
        val IConfigurerBuilder: config: Funogram.Types.BotConfig -> AppEnv.ICfg

