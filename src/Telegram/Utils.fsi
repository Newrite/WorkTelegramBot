namespace WorkTelegram.Telegram
    
    module Utils =
        
        val sendMessageMarkup:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            text: string ->
            markup: Funogram.Telegram.Types.Markup ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val sendMessage:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            text: string ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val editMessageTextBase:
          env: 'a ->
            message: WorkTelegram.Core.Types.TelegramMessage ->
            text: string ->
            Result<Funogram.Telegram.Types.EditMessageResult,
                   Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val editMessageTextBaseMarkup:
          env: 'a ->
            text: string ->
            message: WorkTelegram.Core.Types.TelegramMessage ->
            markup: Funogram.Telegram.Types.InlineKeyboardMarkup ->
            Result<Funogram.Telegram.Types.EditMessageResult,
                   Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val deleteMessageBase:
          env: 'a ->
            message: WorkTelegram.Core.Types.TelegramMessage ->
            Result<bool,Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val sendMessageAndDeleteAfterDelay:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            text: string -> delay: int -> unit
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val sendDocument:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            fileName: string ->
            fileStream: System.IO.Stream ->
            Result<Funogram.Telegram.Types.Message,
                   Funogram.Types.ApiResponseError>
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog
        
        val sendDocumentAndDeleteAfterDelay:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            fileName: string ->
            fileStream: System.IO.Stream -> delay: int -> unit
            when 'a :> WorkTelegram.Infrastructure.ICfg<'b> and
                 'a :> WorkTelegram.Infrastructure.ILog

