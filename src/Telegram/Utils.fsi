namespace WorkTelegram.Telegram
    
    module Utils =
        
        val sendMessageMarkup:
          env: 'a ->
            chatId: Core.UMX.ChatId ->
            text: string ->
            markup: Funogram.Telegram.Types.Markup ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val sendMessage:
          env: 'a ->
            chatId: Core.UMX.ChatId ->
            text: string ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val editMessageTextBase:
          env: 'a ->
            message: Core.Types.TelegramMessage ->
            text: string ->
            Result<Funogram.Telegram.Types.EditMessageResult,
                   Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val editMessageTextBaseMarkup:
          env: 'a ->
            text: string ->
            message: Core.Types.TelegramMessage ->
            markup: Funogram.Telegram.Types.InlineKeyboardMarkup ->
            Result<Funogram.Telegram.Types.EditMessageResult,
                   Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val deleteMessageBase:
          env: 'a ->
            message: Core.Types.TelegramMessage ->
            Result<bool,Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val sendMessageAndDeleteAfterDelay:
          env: 'a ->
            chatId: Core.UMX.ChatId -> text: string -> delay: int -> unit
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val sendDocument:
          env: 'a ->
            chatId: Core.UMX.ChatId ->
            fileName: string ->
            fileStream: System.IO.Stream ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog
        
        val sendDocumentAndDeleteAfterDelay:
          env: 'a ->
            chatId: Core.UMX.ChatId ->
            fileName: string ->
            fileStream: System.IO.Stream -> delay: int -> unit
            when 'a :> Infrastructure.AppEnv.ICfg<'b> and
                 'a :> Infrastructure.AppEnv.ILog

