namespace WorkTelegram.Infrastructure
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type CacheCommand =
        | GetOffices of AsyncReplyChannel<Core.Types.Office list>
        | GetDeletionItems of AsyncReplyChannel<Core.Types.DeletionItem list>
        | GetEmployers of AsyncReplyChannel<Core.Types.Employer list>
        | GetManagers of AsyncReplyChannel<Core.Types.Manager list>
        | GetTelegramMessages of
          AsyncReplyChannel<Core.Types.TelegramMessage list>
        | GetOfficesByManagerId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Office list>
        | TryGetEmployerByChatId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Employer option>
        | TryGetManagerByChatId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Manager option>
        | TryGetTelegramMessageByChatId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.TelegramMessage option>
        | TryAddOffice of
          Core.Types.Office * AsyncReplyChannel<Core.Types.Office option>
        | TryAddEmployer of
          Core.Types.Employer * AsyncReplyChannel<Core.Types.Employer option>
        | TryAddManager of
          Core.Types.Manager * AsyncReplyChannel<Core.Types.Manager option>
        | TryAddDeletionItem of
          Core.Types.DeletionItem *
          AsyncReplyChannel<Core.Types.DeletionItem option>
        | TryAddOrUpdateTelegramMessage of
          Core.Types.TelegramMessage * AsyncReplyChannel<bool>
        | TryChangeEmployerApproved of
          Core.Types.Employer * bool * AsyncReplyChannel<bool>
        | TryDeletionDeletionItemsOfOffice of
          Core.UMX.OfficeId * AsyncReplyChannel<ExtBool>
        | TryDeleteDeletionItem of Core.UMX.DeletionId * AsyncReplyChannel<bool>
        | TryDeleteOffice of Core.UMX.OfficeId * AsyncReplyChannel<bool>
        | TryDeleteTelegramMessage of Core.UMX.ChatId * AsyncReplyChannel<bool>
        | IsApprovedEmployer of Core.Types.Employer * AsyncReplyChannel<bool>
    
    [<NoComparison>]
    type CacheContext =
        {
          Database: AppEnv.IDb
          Logger: AppEnv.ILog
        }
    
    [<NoComparison>]
    type private Cache =
        {
          Employers: Map<Core.UMX.ChatId,Core.Types.Employer>
          Offices: Map<Core.UMX.OfficeId,Core.Types.Office>
          Managers: Map<Core.UMX.ChatId,Core.Types.Manager>
          DeletionItems: Map<Core.UMX.DeletionId,Core.Types.DeletionItem>
          Messages: Map<Core.UMX.ChatId,Core.Types.TelegramMessage>
        }
    
    exception private CacheUnmatchedException of string
    
    module Cache =
        
        val inline private line: unit -> string
        
        val private errorHandler:
          env: AppEnv.ILog -> error: Core.Types.AppError -> source: 'a -> unit
        
        val private initializationCache: ctx: CacheContext -> Cache
        
        val cacheActor:
          ctx: CacheContext -> inbox: MailboxProcessor<CacheCommand>
            -> Async<'a>
        
        val inline private reply:
          env: #AppEnv.ICache<'b>
          -> asyncReplyChannel: (AsyncReplyChannel<'c> -> 'b) -> 'c
        
        val getOffices:
          env: 'a -> Core.Types.Office list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getOfficesAsync:
          env: 'a -> System.Threading.Tasks.Task<Core.Types.Office list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getDeletionItems:
          env: 'a -> Core.Types.DeletionItem list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getDeletionItemsAsync:
          env: 'a -> System.Threading.Tasks.Task<Core.Types.DeletionItem list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getEmployers:
          env: 'a -> Core.Types.Employer list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getEmployersAsync:
          env: 'a -> System.Threading.Tasks.Task<Core.Types.Employer list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getManagers:
          env: 'a -> Core.Types.Manager list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getMangersAsync:
          env: 'a -> System.Threading.Tasks.Task<Core.Types.Manager list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getTelegramMessages:
          env: 'a -> Core.Types.TelegramMessage list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getTelegramMessagesAsync:
          env: 'a
            -> System.Threading.Tasks.Task<Core.Types.TelegramMessage list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getOfficesByManagerId:
          env: 'a -> chatId: Core.UMX.ChatId -> Core.Types.Office list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getOfficesByManagerIdAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.Office list>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetEmployerByChatId:
          env: 'a -> chatId: Core.UMX.ChatId -> Core.Types.Employer option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetEmployerByChatIdAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.Employer option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetManagerByChatId:
          env: 'a -> chatId: Core.UMX.ChatId -> Core.Types.Manager option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetManagerByChatIdAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.Manager option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetTelegramMessageByChatId:
          env: 'a -> chatId: Core.UMX.ChatId
            -> Core.Types.TelegramMessage option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetTelegramMessageByChatIdAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.TelegramMessage option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOffice:
          env: 'a -> office: Core.Types.Office -> Core.Types.Office option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOfficeAsync:
          env: 'a -> recordOffice: Core.Types.Office
            -> System.Threading.Tasks.Task<Core.Types.Office option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddEmployer:
          env: 'a -> employer: Core.Types.Employer -> Core.Types.Employer option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddEmployerAsync:
          env: 'a -> recordEmployer: Core.Types.Employer
            -> System.Threading.Tasks.Task<Core.Types.Employer option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddManager:
          env: 'a -> manager: Core.Types.Manager -> Core.Types.Manager option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddManagerAsync:
          env: 'a -> managerDto: Core.Types.Manager
            -> System.Threading.Tasks.Task<Core.Types.Manager option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddDeletionItem:
          env: 'a -> deletionItem: Core.Types.DeletionItem
            -> Core.Types.DeletionItem option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddDeletionItemAsync:
          env: 'a -> recordDeletionItem: Core.Types.DeletionItem
            -> System.Threading.Tasks.Task<Core.Types.DeletionItem option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryChangeEmployerApproved:
          env: 'a -> employer: Core.Types.Employer -> isApproved: bool -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryChangeEmployerApprovedAsync:
          env: 'a -> employer: Core.Types.Employer -> isApproved: bool
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeletionDeletionItemsOfOffice:
          env: 'a -> officeId: Core.UMX.OfficeId -> ExtBool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeletionDeletionItemsOfOfficeAsync:
          env: 'a -> officeId: Core.UMX.OfficeId
            -> System.Threading.Tasks.Task<ExtBool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteDeletionItem:
          env: 'a -> deletionId: Core.UMX.DeletionId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteDeletionItemAsync:
          env: 'a -> deletionId: Core.UMX.DeletionId
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteOffice:
          env: 'a -> officeId: Core.UMX.OfficeId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteOfficeAsync:
          env: 'a -> officeId: Core.UMX.OfficeId
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOrUpdateTelegramMessage:
          env: 'a -> message: Core.Types.TelegramMessage -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOrUpdateTelegramMessageAsync:
          env: 'a -> message: Core.Types.TelegramMessage
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteTelegramMessage:
          env: 'a -> chatId: Core.UMX.ChatId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteTelegramMessageAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val isApprovedEmployer:
          env: 'a -> employer: Core.Types.Employer -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val isApprovedEmployerAsync:
          env: 'a -> employer: Core.Types.Employer
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val ICacheBuilder: mailbox: MailboxProcessor<'a> -> AppEnv.ICache<'a>

