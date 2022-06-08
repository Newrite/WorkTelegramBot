namespace WorkTelegram.Infrastructure
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type CacheCommand =
        | GetOffices of AsyncReplyChannel<Core.Types.Office list>
        | GetDeletionItems of AsyncReplyChannel<Core.Types.DeletionItem list>
        | GetEmployers of AsyncReplyChannel<Core.Types.Employer list>
        | GetManagers of AsyncReplyChannel<Core.Types.Manager list>
        | GetMessages of AsyncReplyChannel<Funogram.Telegram.Types.Message list>
        | GetOfficesByManagerId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Office list>
        | TryGetEmployerByChatId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Employer option>
        | TryGetManagerByChatId of
          Core.UMX.ChatId * AsyncReplyChannel<Core.Types.Manager option>
        | TryGetMessageByChatId of
          Core.UMX.ChatId *
          AsyncReplyChannel<Funogram.Telegram.Types.Message option>
        | TryAddOfficeInDb of
          Core.RecordOffice * AsyncReplyChannel<Core.Types.Office option>
        | TryAddEmployerInDb of
          Core.RecordEmployer * AsyncReplyChannel<Core.Types.Employer option>
        | TryAddManagerInDb of
          Core.ManagerDto * AsyncReplyChannel<Core.Types.Manager option>
        | TryAddDeletionItemInDb of
          Core.RecordDeletionItem *
          AsyncReplyChannel<Core.Types.DeletionItem option>
        | TryAddOrUpdateMessageInDb of
          Funogram.Telegram.Types.Message * AsyncReplyChannel<bool>
        | TryUpdateEmployerApprovedInDb of
          Core.Types.Employer * bool * AsyncReplyChannel<bool>
        | TrySetDeletionOnItemsOfOffice of
          Core.UMX.OfficeId * AsyncReplyChannel<bool>
        | TryHideDeletionItem of Core.UMX.DeletionId * AsyncReplyChannel<bool>
        | TryDeleteOffice of Core.UMX.OfficeId * AsyncReplyChannel<bool>
        | TryDeleteMessageJson of Core.UMX.ChatId * AsyncReplyChannel<bool>
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
          Employers: Core.Types.Employer list
          Offices: Core.Types.Office list
          Managers: Core.Types.Manager list
          DeletionItems: Core.Types.DeletionItem list
          Messages: Funogram.Telegram.Types.Message list
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
        
        val getMessages:
          env: 'a -> Funogram.Telegram.Types.Message list
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val getMessagesAsync:
          env: 'a
            -> System.Threading.Tasks.Task<Funogram.Telegram.Types.Message list>
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
        
        val tryGetMessageByChatId:
          env: 'a -> chatId: Core.UMX.ChatId
            -> Funogram.Telegram.Types.Message option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryGetMessageByChatIdAsync:
          env: 'a -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Funogram.Telegram.Types.Message option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOfficeInDb:
          env: 'a -> recordOffice: Core.RecordOffice -> Core.Types.Office option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOfficeInDbAsync:
          env: 'a -> recordOffice: Core.RecordOffice
            -> System.Threading.Tasks.Task<Core.Types.Office option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddEmployerInDb:
          env: 'a -> recordEmployer: Core.RecordEmployer
            -> Core.Types.Employer option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddEmployerInDbAsync:
          env: 'a -> recordEmployer: Core.RecordEmployer
            -> System.Threading.Tasks.Task<Core.Types.Employer option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddManagerInDb:
          env: 'a -> managerDto: Core.ManagerDto -> Core.Types.Manager option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddManagerInDbAsync:
          env: 'a -> managerDto: Core.ManagerDto
            -> System.Threading.Tasks.Task<Core.Types.Manager option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddDeletionItemInDb:
          env: 'a -> recordDeletionItem: Core.RecordDeletionItem
            -> Core.Types.DeletionItem option
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddDeletionItemInDbAsync:
          env: 'a -> recordDeletionItem: Core.RecordDeletionItem
            -> System.Threading.Tasks.Task<Core.Types.DeletionItem option>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryUpdateEmployerApprovedInDb:
          env: 'a -> employer: Core.Types.Employer -> isApproved: bool -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryUpdateEmployerApprovedInDbAsync:
          env: 'a -> employer: Core.Types.Employer -> isApproved: bool
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val trySetDeletionOnItemsOfOffice:
          env: 'a -> officeId: Core.UMX.OfficeId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val trySetDeletionOnItemsOfOfficeAsync:
          env: 'a -> officeId: Core.UMX.OfficeId
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryHideDeletionItem:
          env: 'a -> deletionId: Core.UMX.DeletionId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryHideDeletionItemAsync:
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
        
        val tryAddOrUpdateMessageInDb:
          env: 'a -> message: Funogram.Telegram.Types.Message -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryAddOrUpdateMessageInDbAsync:
          env: 'a -> message: Funogram.Telegram.Types.Message
            -> System.Threading.Tasks.Task<bool>
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteMessageJson:
          env: 'a -> chatId: Core.UMX.ChatId -> bool
            when 'a :> AppEnv.ILog and 'a :> AppEnv.ICache<CacheCommand>
        
        val tryDeleteMessageJsonAsync:
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

