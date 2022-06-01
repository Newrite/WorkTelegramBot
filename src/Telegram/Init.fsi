namespace WorkTelegram.Telegram
    
    module Init =
        
        val init:
          env: Core.Types.Env -> history: System.Collections.Generic.Stack<'a>
          -> message: Funogram.Telegram.Types.Message -> CoreModel

