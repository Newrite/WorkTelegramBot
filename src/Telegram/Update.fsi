namespace WorkTelegram.Telegram
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type UpdateMessage =
        | AuthManagerChange of AuthProcess.AuthManager
        | FinishEmployerAuth of Core.Types.Employer
        | FinishManagerAuth of Core.Types.Manager
        | ManagerChooseOffice of
          ManagerProcess.ManagerContext * Core.Types.Office
        | ManagerMakeOfficeChange of
          ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
        | FinishMakeOfficeProcess of Core.Types.Office
        | StartEditDeletionItems of EmployerProcess.EmployerContext
        | StartAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.Office
        | StartDeAuthEmployers of
          ManagerProcess.ManagerContext * Core.Types.Office
        | DeletionProcessChange of
          EmployerProcess.EmployerContext * EmployerProcess.Deletion
        | AuthEmployerChange of AuthProcess.AuthEmployer
        | FinishDeletionProcess of
          EmployerProcess.EmployerContext * Core.Types.DeletionItem
        | Back
        | Cancel
        | ReRender
    
    module Update =
        
        val update:
          env: 'a ->
            message: UpdateMessage ->
            model: Model.ModelContext<Model.CoreModel> ->
            callInitModelFunction: (unit -> Model.ModelContext<Model.CoreModel>) ->
            Model.ModelContext<Model.CoreModel>
            when 'a :> Infrastructure.IRep<Infrastructure.CacheCommand> and
                 'a :> Infrastructure.ILog and 'a :> Infrastructure.IDb and
                 'a :> Infrastructure.ICfg<'b>

