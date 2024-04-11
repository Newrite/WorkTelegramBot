namespace WorkTelegram.Infrastructure
    
    type IRepository<'CacheCommand> =
        
        abstract Cache: Agent<'CacheCommand>
    
    type IRep<'CacheCommand> =
        
        abstract Repository: IRepository<'CacheCommand>
    
    module Repository =
        
        exception private RepositoryUnmatchedException of string
        
        val private cache: env: #IRep<'b> -> Agent<'b>
        
        val errorHandler:
          env: ILog -> error: WorkTelegram.Core.Types.AppError -> unit
        
        val IRepBuilder: cache: Agent<'a> -> IRep<'a>
        
        val offices:
          env: 'a -> OfficesMap when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val managers:
          env: 'a -> ManagersMap when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val employers:
          env: 'a -> EmployersMap when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val messages:
          env: 'a -> MessagesMap when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val deletionItems:
          env: 'a -> DeletionItemsMap
            when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val tryEmployerByChatId:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            WorkTelegram.Core.Types.Employer option
            when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val tryManagerByChatId:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            WorkTelegram.Core.Types.Manager option
            when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val tryMessageByChatId:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            WorkTelegram.Core.Types.TelegramMessage option
            when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val tryOfficeByChatId:
          env: 'a ->
            chatId: WorkTelegram.Core.UMX.ChatId ->
            Map<WorkTelegram.Core.UMX.OfficeId,WorkTelegram.Core.Types.Office>
            when 'a :> ILog and 'a :> IRep<CacheCommand>
        
        val tryAddOffice:
          env: 'a -> office: WorkTelegram.Core.Types.Office -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryAddMessage:
          env: 'a -> message: WorkTelegram.Core.Types.TelegramMessage -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryAddEmployer:
          env: 'a -> employer: WorkTelegram.Core.Types.Employer -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryAddManager:
          env: 'a -> manager: WorkTelegram.Core.Types.Manager -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryAddDeletionItem:
          env: 'a -> deletionItem: WorkTelegram.Core.Types.DeletionItem -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryAddChatId:
          env: 'a -> chatId: WorkTelegram.Core.UMX.ChatId -> bool
            when 'a :> ILog and 'a :> IDb
        
        val tryDeleteMessage:
          env: 'a -> message: WorkTelegram.Core.Types.TelegramMessage -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryUpdateMessage:
          env: 'a -> message: WorkTelegram.Core.Types.TelegramMessage -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryDeleteOffice:
          env: 'a -> office: WorkTelegram.Core.Types.Office -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryUpdateOffice:
          env: 'a -> office: WorkTelegram.Core.Types.Office -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryUpdateEmployer:
          env: 'a -> employer: WorkTelegram.Core.Types.Employer -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryUpdateDeletionItems:
          env: 'a ->
            deletionItems: WorkTelegram.Core.Types.DeletionItem list -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb
        
        val tryUpdateDeletionItem:
          env: 'a -> deletionItem: WorkTelegram.Core.Types.DeletionItem -> bool
            when 'a :> IRep<CacheCommand> and 'a :> ILog and 'a :> IDb

