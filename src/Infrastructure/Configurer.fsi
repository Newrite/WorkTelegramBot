namespace WorkTelegram.Infrastructure
    
    module Configurer =
        
        val botConfig: env: #AppEnv.ICfg<'b> -> Funogram.Types.BotConfig
        
        val elmishDict:
          env: #AppEnv.ICfg<'b> ->
            System.Collections.Concurrent.ConcurrentDictionary<int64,'b>
        
        val IConfigurerBuilder:
          config: Funogram.Types.BotConfig ->
            dict: System.Collections.Concurrent.ConcurrentDictionary<int64,'a> ->
            AppEnv.ICfg<'a>

