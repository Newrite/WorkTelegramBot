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
        | RemoveOffice of WorkTelegram.Core.Types.Office
        | RemoveTelegramMessage of WorkTelegram.Core.Types.TelegramMessage
        | UpdateOrAddOffice of WorkTelegram.Core.Types.Office
        | UpdateOrAddEmployer of WorkTelegram.Core.Types.Employer
        | UpdateOrAddManager of WorkTelegram.Core.Types.Manager
        | UpdateOrAddDeletionItem of WorkTelegram.Core.Types.DeletionItem
        | UpdateOrAddDeletionItems of WorkTelegram.Core.Types.DeletionItem list
        | UpdateOrAddTelegramMessage of WorkTelegram.Core.Types.TelegramMessage
    
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
            errorHandler: ('a -> WorkTelegram.Core.Types.AppError -> unit) ->
            Cache
        
        val cacheAgent:
          cache: Cache -> (CacheCommand -> System.Threading.Tasks.Task<unit>)

