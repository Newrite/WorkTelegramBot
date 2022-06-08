namespace WorkTelegram.Telegram
    
    module AuthProcess =
        
        [<RequireQualifiedAccess>]
        type Employer =
            | EnteringOffice
            | EnteringLastFirstName of Core.Types.Office
            | AskingFinish of Core.RecordEmployer
        
        [<RequireQualifiedAccess>]
        type Manager =
            | EnteringLastFirstName
            | AskingFinish of Core.ManagerDto
        
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
            | AskingFinish of Core.RecordDeletionItem
        
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
            | AskingFinish of Core.RecordOffice
        
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
    
    [<RequireQualifiedAccess>]
    type CoreModel =
        | Employer of EmployerProcess.EmployerContext
        | Manager of ManagerProcess.ManagerContext
        | Auth of AuthProcess.Model
        | Error of string

