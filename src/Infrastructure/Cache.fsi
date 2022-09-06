namespace WorkTelegram.Infrastructure
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type CacheCommand =
        | Offices of System.Threading.Tasks.TaskCompletionSource<OfficesMap>
        | DeletionItems of
          System.Threading.Tasks.TaskCompletionSource<DeletionItemsMap>
        | Employers of System.Threading.Tasks.TaskCompletionSource<EmployersMap>
        | Managers of System.Threading.Tasks.TaskCompletionSource<ManagersMap>
        | TelegramMessages of
          System.Threading.Tasks.TaskCompletionSource<MessagesMap>
        | RemoveOffice of Core.Types.Office
        | RemoveTelegramMessage of Core.Types.TelegramMessage
        | UpdateOrAddOffice of Core.Types.Office
        | UpdateOrAddEmployer of Core.Types.Employer
        | UpdateOrAddManager of Core.Types.Manager
        | UpdateOrAddDeletionItem of Core.Types.DeletionItem
        | UpdateOrAddDeletionItems of Core.Types.DeletionItem list
        | UpdateOrAddTelegramMessage of Core.Types.TelegramMessage
    
    [<NoComparison>]
    type Cache =
        {
          mutable Employers: EmployersMap
          mutable Offices: OfficesMap
          mutable Managers: ManagersMap
          mutable DeletionItems: DeletionItemsMap
          mutable Messages: MessagesMap
        }
    
    module Cache =
        
        val initializationCache:
          logger: 'a ->
            database: IDb ->
            errorHandler: ('a -> Core.Types.AppError -> unit) -> Cache
        
        val cacheAgent:
          cache: Cache -> (CacheCommand -> System.Threading.Tasks.Task<unit>)

