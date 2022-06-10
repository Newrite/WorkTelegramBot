namespace WorkTelegram.Telegram
    
    module Init =
        
        exception private NegativeOfficesCountException of string
        
        val init:
          env: 'a -> history: System.Collections.Generic.Stack<'b>
          -> message: Funogram.Telegram.Types.Message -> CoreModel
            when 'a :> Infrastructure.AppEnv.ILog and
                 'a :> Infrastructure.AppEnv.ICache<Infrastructure.CacheCommand> and
                 'a :> Infrastructure.AppEnv.IDb
