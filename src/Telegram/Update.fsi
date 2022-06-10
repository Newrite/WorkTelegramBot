namespace WorkTelegram.Telegram
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type UpdateMessage =
        | AuthManagerChange of AuthProcess.Manager
        | FinishEmployerAuth of Core.RecordEmployer
        | FinishManagerAuth of Core.ManagerDto
        | ManagerChooseOffice of
          ManagerProcess.ManagerContext * Core.Types.Office
        | ManagerMakeOfficeChange of
          ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
        | FinishMakeOfficeProcess of Core.RecordOffice
        | StartEditDeletionItems of EmployerProcess.EmployerContext
        | StartAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.Office
        | StartDeAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.Office
        | DeletionProcessChange of
          EmployerProcess.EmployerContext * EmployerProcess.Deletion
        | AuthEmployerChange of AuthProcess.Employer
        | FinishDeletionProcess of
          EmployerProcess.EmployerContext * Core.RecordDeletionItem
        | Back
        | Cancel
        | ReRender
    
    module Update =
        
        val update:
          env: 'a -> history: System.Collections.Generic.Stack<Model.CoreModel>
          -> message: UpdateMessage -> model: Model.CoreModel
          -> callInitModelFunction: (unit -> Model.CoreModel) -> Model.CoreModel
            when 'a :> Infrastructure.AppEnv.ILog and
                 'a :> Infrastructure.AppEnv.ICache<Infrastructure.CacheCommand> and
                 'a :> Infrastructure.AppEnv.ICfg

