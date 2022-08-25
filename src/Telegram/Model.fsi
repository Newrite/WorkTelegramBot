namespace WorkTelegram.Telegram
    
    module AuthProcess =
        
        [<RequireQualifiedAccess>]
        type AuthEmployer =
            | EnteringOffice
            | EnteringLastFirstName of Core.Types.Office
            | AskingFinish of Core.Types.Employer
        
        [<RequireQualifiedAccess>]
        type AuthManager =
            | EnteringLastFirstName
            | AskingFinish of Core.Types.Manager
        
        [<RequireQualifiedAccess>]
        type AuthModel =
            | Employer of AuthEmployer
            | Manager of AuthManager
            | NoAuth
    
    module EmployerProcess =
        
        [<RequireQualifiedAccess>]
        type Deletion =
            | EnteringName
            | EnteringMac of Core.Types.ItemWithSerial
            | EnteringSerial of Core.Types.ItemWithOnlyName
            | EnteringCount of Core.Types.Item
            | EnteringLocation of Core.Types.Item * Core.Types.PositiveInt
            | AskingFinish of Core.Types.DeletionItem
        
        [<RequireQualifiedAccess>]
        type EmployerModel =
            | Deletion of Deletion
            | WaitChoice
            | EditDeletionItems
        
        type EmployerContext =
            {
              Employer: Core.Types.Employer
              Model: EmployerModel
            }
            
            member UpdateModel: model: EmployerModel -> EmployerContext
    
    module ManagerProcess =
        
        [<RequireQualifiedAccess>]
        type MakeOffice =
            | EnteringName
            | AskingFinish of Core.Types.Office
        
        [<RequireQualifiedAccess>]
        type ManagerModel =
            | NoOffices
            | MakeOffice of MakeOffice
            | ChooseOffice of Core.Types.Office list
            | InOffice of Core.Types.Office
            | AuthEmployers of Core.Types.Office
            | DeAuthEmployers of Core.Types.Office
        
        type ManagerContext =
            {
              Manager: Core.Types.Manager
              Model: ManagerModel
            }
            
            member UpdateModel: model: ManagerModel -> ManagerContext
    
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
                      message: Core.Types.TelegramMessage ->
                      ModelContext<CoreModel>
                      when 'a :> Infrastructure.AppEnv.ILog and
                           'a :>
                                Infrastructure.AppEnv.ICache<Infrastructure.CacheCommand> and
                           'a :> Infrastructure.AppEnv.IDb
            
            member Transform: model: CoreModel -> ModelContext<'Model>

