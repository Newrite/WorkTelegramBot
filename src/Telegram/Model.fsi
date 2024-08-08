namespace WorkTelegram.Telegram
    
    module AuthProcess =
        
        [<RequireQualifiedAccess>]
        type AuthEmployer =
            | EnteringOffice
            | EnteringLastFirstName of WorkTelegram.Core.Types.Office
            | AskingFinish of WorkTelegram.Core.Types.Employer
        
        [<RequireQualifiedAccess>]
        type AuthManager =
            | EnteringLastFirstName
            | AskingFinish of WorkTelegram.Core.Types.Manager
        
        [<RequireQualifiedAccess>]
        type AuthModel =
            | Employer of AuthEmployer
            | Manager of AuthManager
            | NoAuth
    
    module EmployerProcess =
        
        [<RequireQualifiedAccess>]
        type Deletion =
            | EnteringName
            | EnteringMac of WorkTelegram.Core.Types.ItemWithSerial
            | EnteringSerial of WorkTelegram.Core.Types.ItemWithOnlyName
            | EnteringCount of WorkTelegram.Core.Types.Item
            | EnteringLocation of
              WorkTelegram.Core.Types.Item * WorkTelegram.Core.Types.PositiveInt
            | AskingFinish of WorkTelegram.Core.Types.DeletionItem
        
        [<RequireQualifiedAccess>]
        type EmployerModel =
            | Deletion of Deletion
            | WaitChoice
            | EditDeletionItems
            | ShowedLastRecords
        
        type EmployerContext =
            {
              Employer: WorkTelegram.Core.Types.Employer
              Model: EmployerModel
            }
            
            member UpdateModel: model: EmployerModel -> EmployerContext
    
    module ManagerProcess =
        
        [<RequireQualifiedAccess>]
        type MakeOffice =
            | EnteringName
            | AskingFinish of WorkTelegram.Core.Types.Office
        
        [<RequireQualifiedAccess>]
        type ManagerModel =
            | NoOffices
            | MakeOffice of MakeOffice
            | ChooseOffice of WorkTelegram.Infrastructure.OfficesMap
            | InOffice of WorkTelegram.Core.Types.Office
            | AuthEmployers of WorkTelegram.Core.Types.Office
            | DeAuthEmployers of WorkTelegram.Core.Types.Office
            | DelegateOffice of WorkTelegram.Core.Types.Office
            | EmployerOperations of WorkTelegram.Core.Types.Office
            | OfficeOperations of WorkTelegram.Core.Types.Office
            | DelegateEmployer of WorkTelegram.Core.Types.Office
            | DelegateEmployerChooseOffice of
              WorkTelegram.Core.Types.Office * WorkTelegram.Core.Types.Employer
        
        type ManagerContext =
            {
              Manager: WorkTelegram.Core.Types.Manager
              Model: ManagerModel
            }
            
            member UpdateModel: model: ManagerModel -> ManagerContext
    
    [<AutoOpen>]
    module Model =
        
        exception private NegativeOfficesCountException of string
        
        [<RequireQualifiedAccess>]
        type CoreModel =
            | Employer of EmployerProcess.EmployerContext
            | Manager of ManagerProcess.ManagerContext
            | Auth of AuthProcess.AuthModel
            | Error of string
        
        [<NoComparison>]
        type ModelContext<'Model> =
            {
              History: System.Collections.Generic.Stack<'Model>
              Model: CoreModel
            }
            
            static member
              Init: env: 'a ->
                      message: WorkTelegram.Core.Types.TelegramMessage ->
                      ModelContext<CoreModel>
                      when 'a :> WorkTelegram.Infrastructure.ILog and
                           'a :>
                                WorkTelegram.Infrastructure.IRep<WorkTelegram.Infrastructure.CacheCommand> and
                           'a :> WorkTelegram.Infrastructure.IDb and
                           'a :> WorkTelegram.Infrastructure.ICfg<'b>
            
            member Transform: model: CoreModel -> ModelContext<'Model>

