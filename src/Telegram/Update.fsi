namespace WorkTelegram.Telegram
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type UpdateMessage =
        | AuthManagerChange of AuthProcess.AuthManager
        | FinishEmployerAuth of WorkTelegram.Core.Types.Employer
        | FinishManagerAuth of WorkTelegram.Core.Types.Manager
        | ManagerChooseOffice of
          ManagerProcess.ManagerContext * WorkTelegram.Core.Types.Office
        | ManagerMakeOfficeChange of
          ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
        | FinishMakeOfficeProcess of WorkTelegram.Core.Types.Office
        | StartEditDeletionItems of EmployerProcess.EmployerContext
        | StartAuthEmployers of
          ManagerProcess.ManagerContext * WorkTelegram.Core.Types.Office
        | StartDeAuthEmployers of
          ManagerProcess.ManagerContext * WorkTelegram.Core.Types.Office
        | DeletionProcessChange of
          EmployerProcess.EmployerContext * EmployerProcess.Deletion
        | AuthEmployerChange of AuthProcess.AuthEmployer
        | FinishDeletionProcess of
          EmployerProcess.EmployerContext * WorkTelegram.Core.Types.DeletionItem
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
            when 'a :>
                      WorkTelegram.Infrastructure.IRep<WorkTelegram.Infrastructure.CacheCommand> and
                 'a :> WorkTelegram.Infrastructure.ILog and
                 'a :> WorkTelegram.Infrastructure.IDb and
                 'a :> WorkTelegram.Infrastructure.ICfg<'b>

