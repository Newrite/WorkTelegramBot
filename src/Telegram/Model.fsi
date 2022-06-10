namespace WorkTelegram.Telegram
    
    module AuthProcess =
        
        [<RequireQualifiedAccess>]
        type Employer =
            | EnteringOffice
            | EnteringLastFirstName of Core.Types.Office
            | AskingFinish of Core.Types.Employer
        
        [<RequireQualifiedAccess>]
        type Manager =
            | EnteringLastFirstName
            | AskingFinish of Core.Types.Manager
        
        [<RequireQualifiedAccess>]
        type Model =
            | Employer of Employer
            | Manager of Manager
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
        type Model =
            | Deletion of Deletion
            | WaitChoice
            | EditDeletionItems
        
        type EmployerContext =
            {
              Employer: Core.Types.Employer
              Model: Model
            }
            
            member UpdateModel: model: Model -> EmployerContext
    
    module ManagerProcess =
        
        [<RequireQualifiedAccess>]
        type MakeOffice =
            | EnteringName
            | AskingFinish of Core.Types.Office
        
        [<RequireQualifiedAccess>]
        type Model =
            | NoOffices
            | MakeOffice of MakeOffice
            | ChooseOffice of Core.Types.Office list
            | InOffice of Core.Types.Office
            | AuthEmployers of Core.Types.Office
            | DeAuthEmployers of Core.Types.Office
        
        type ManagerContext =
            {
              Manager: Core.Types.Manager
              Model: Model
            }
            
            member UpdateModel: model: Model -> ManagerContext
    
    module Model =
        
        exception private NegativeOfficesCountException of string
        
        [<RequireQualifiedAccess>]
        type CoreModel =
            | Employer of EmployerProcess.EmployerContext
            | Manager of ManagerProcess.ManagerContext
            | Auth of AuthProcess.Model
            | Error of string
            
            static member
              Init: env: 'a -> history: System.Collections.Generic.Stack<'b>
                    -> message: Funogram.Telegram.Types.Message -> CoreModel
                      when 'a :> Infrastructure.AppEnv.ILog and
                           'a :>
                                Infrastructure.AppEnv.ICache<Infrastructure.CacheCommand> and
                           'a :> Infrastructure.AppEnv.IDb

