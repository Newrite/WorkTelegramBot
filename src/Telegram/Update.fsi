namespace WorkTelegram.Telegram
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type UpdateMessage =
        | AuthManagerChange of AuthProcess.Manager
        | FinishEmployerAuth of Core.Types.RecordedEmployer * Core.Types.Env
        | FinishManagerAuth of Core.Types.RecordedManager * Core.Types.Env
        | ManagerChooseOffice of
          ManagerProcess.ManagerContext * Core.Types.RecordedOffice
        | ManagerMakeOfficeChange of
          ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
        | FinishMakeOfficeProcess of Core.Types.RecordedOffice * Core.Types.Env
        | StartEditRecordedItems of EmployerProcess.EmployerContext
        | StartAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.RecordedOffice
        | StartDeAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.RecordedOffice
        | DeletionProcessChange of
          EmployerProcess.EmployerContext * EmployerProcess.Deletion
        | AuthEmployerChange of AuthProcess.Employer
        | FinishDeletionProcess of
          EmployerProcess.EmployerContext * Core.Types.RecordedDeletionItem *
          Core.Types.Env
        | Back
        | Cancel
        | NothingChange
    
    module Update =
        
        val update:
          history: System.Collections.Generic.Stack<CoreModel>
          -> message: UpdateMessage -> model: CoreModel
          -> callInitilizationModelFunction: (unit -> CoreModel) -> CoreModel

