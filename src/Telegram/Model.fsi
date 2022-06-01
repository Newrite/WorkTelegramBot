namespace WorkTelegram.Telegram
    
    module AuthProcess =
        
        [<RequireQualifiedAccess>]
        type Employer =
            | EnteringOffice
            | EnteringLastFirstName of Core.Types.RecordedOffice
            | AskingFinish of Core.Types.RecordedEmployer
        
        [<RequireQualifiedAccess>]
        type Manager =
            | EnteringLastFirstName
            | AskingFinish of Core.Types.RecordedManager
        
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
            | AskingFinish of Core.Types.RecordedDeletionItem
        
        [<RequireQualifiedAccess>]
        type Model =
            | Deletion of Deletion
            | WaitChoice
            | EditRecordedDeletions
        
        type EmployerContext =
            {
              Employer: Core.Types.RecordedEmployer
              Model: Model
            }
            
            member UpdateModel: model: Model -> EmployerContext
    
    module ManagerProcess =
        
        [<RequireQualifiedAccess>]
        type MakeOffice =
            | EnteringName
            | AskingFinish of Core.Types.RecordedOffice
        
        [<RequireQualifiedAccess>]
        type Model =
            | NoOffices
            | MakeOffice of MakeOffice
            | ChooseOffice of Core.Types.RecordedOffice list
            | InOffice of Core.Types.RecordedOffice
            | AuthEmployers of Core.Types.RecordedOffice
            | DeAuthEmployers of Core.Types.RecordedOffice
        
        type ManagerContext =
            {
              Manager: Core.Types.RecordedManager
              Model: Model
            }
            
            member UpdateModel: model: Model -> ManagerContext
    
    [<RequireQualifiedAccess>]
    type CoreModel =
        | Employer of EmployerProcess.EmployerContext
        | Manager of ManagerProcess.ManagerContext
        | Auth of AuthProcess.Model
        | Error of string

