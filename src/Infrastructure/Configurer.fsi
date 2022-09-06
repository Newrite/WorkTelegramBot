namespace WorkTelegram.Infrastructure
    
    type IConfigurer<'ElmishCommand> =
        
        abstract BotConfig: Funogram.Types.BotConfig
        
        abstract
          ElmishDict: System.Collections.Concurrent.ConcurrentDictionary<int64,
                                                                         Agent<'ElmishCommand>>
    
    type ICfg<'ElmishCommand> =
        
        abstract Configurer: IConfigurer<'ElmishCommand>
    
    module Configurer =
        
        val botConfig: env: #ICfg<'b> -> Funogram.Types.BotConfig
        
        val elmishDict:
          env: #ICfg<'b> ->
            System.Collections.Concurrent.ConcurrentDictionary<int64,Agent<'b>>
        
        val IConfigurerBuilder:
          config: Funogram.Types.BotConfig ->
            dict: System.Collections.Concurrent.ConcurrentDictionary<int64,
                                                                     Agent<'a>> ->
            ICfg<'a>

