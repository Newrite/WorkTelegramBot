namespace WorkTelegram.Infrastructure
    
    module Cache =
        
        val cacheActor:
          inbox: MailboxProcessor<Core.Types.CacheCommand> -> Async<'a>
        
        val private reply:
          env: Core.Types.Env
          -> asyncReplyChannel: (AsyncReplyChannel<'a option>
                                   -> Core.Types.CacheCommand) -> 'a option
        
        val employerByChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Core.Types.RecordedEmployer option
        
        val employerByChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.RecordedEmployer option>
        
        val managerByChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Core.Types.RecordedManager option
        
        val managerByChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.RecordedManager option>
        
        val offices:
          env: Core.Types.Env -> Core.Types.RecordedOffice list option
        
        val officesAsync:
          env: Core.Types.Env
            -> System.Threading.Tasks.Task<Core.Types.RecordedOffice list option>
        
        val officeEmployers:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> Core.Types.RecordedEmployer list option
        
        val officeEmployersAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<Core.Types.RecordedEmployer list option>
        
        val addOffice:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice -> unit
        
        val addOfficeAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<unit>
        
        val addEmployer:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer -> unit
        
        val addEmployerAsync:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer
            -> System.Threading.Tasks.Task<unit>
        
        val addManager:
          env: Core.Types.Env -> manager: Core.Types.RecordedManager -> unit
        
        val addManagerAsync:
          env: Core.Types.Env -> manager: Core.Types.RecordedManager
            -> System.Threading.Tasks.Task<unit>
        
        val initialization: env: Core.Types.Env -> unit

