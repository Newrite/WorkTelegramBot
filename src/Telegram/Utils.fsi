namespace WorkTelegram.Telegram
    
    module Utils =
        
        val sendMessageMarkup:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId -> text: string
          -> markup: Funogram.Telegram.Types.Markup
            -> Result<Funogram.Telegram.Types.Message,
                      Funogram.Types.ApiResponseError>
        
        val sendMessage:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId -> text: string
            -> Result<Funogram.Telegram.Types.Message,
                      Funogram.Types.ApiResponseError>
        
        val editMessageTextBase:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
          -> text: string
            -> Result<Funogram.Telegram.Types.EditMessageResult,
                      Funogram.Types.ApiResponseError>
        
        val editMessageTextBaseMarkup:
          env: Core.Types.Env -> text: string
          -> message: Funogram.Telegram.Types.Message
          -> markup: Funogram.Telegram.Types.InlineKeyboardMarkup
            -> Result<Funogram.Telegram.Types.EditMessageResult,
                      Funogram.Types.ApiResponseError>
        
        val deleteMessageBase:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
            -> Result<Funogram.Telegram.Types.EditMessageResult,
                      Funogram.Types.ApiResponseError>
        
        val sendMessageAndDeleteAfterDelay:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId -> text: string
          -> delay: int -> unit
        
        val sendDocument:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId -> fileName: string
          -> fileStream: System.IO.Stream
            -> Result<Funogram.Telegram.Types.Message,
                      Funogram.Types.ApiResponseError>
        
        val sendDocumentAndDeleteAfterDelay:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId -> fileName: string
          -> fileStream: System.IO.Stream -> delay: int -> unit

