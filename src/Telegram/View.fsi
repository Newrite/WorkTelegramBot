namespace WorkTelegram.Telegram
    
    module View =
        
        exception private ViewUnmatchedException of string
        
        [<NoEquality; NoComparison>]
        type private ViewContext<'ElmishCommand,'CacheCommand> =
            {
              Dispatch: (UpdateMessage -> unit)
              BackCancelKeyboard: Elmish.Keyboard
              AppEnv:
                WorkTelegram.Infrastructure.IAppEnv<'ElmishCommand,'CacheCommand>
              Notify: (WorkTelegram.Core.UMX.ChatId -> string -> int -> unit)
            }
        
        [<RequireQualifiedAccess>]
        module private Functions =
            
            val sendExcelItemsDocumentExn:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                items: WorkTelegram.Core.Types.DeletionItem list -> unit
            
            val enteringLastFirstNameEmployerMessageHandle:
              ctx: ViewContext<'a,'b> ->
                office: WorkTelegram.Core.Types.Office ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringOfficeNameMessageHandle:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringLastFirstNameManagerMessageHandle:
              ctx: ViewContext<'a,'b> ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringNameMessageHandle:
              employerState: EmployerProcess.EmployerContext ->
                ctx: ViewContext<'a,'b> ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringSerialMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: WorkTelegram.Core.Types.ItemWithOnlyName ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringMacAddressMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: WorkTelegram.Core.Types.ItemWithSerial ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringCountMessageHandleExn:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: WorkTelegram.Core.Types.Item ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
            
            val enteringLocationMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: WorkTelegram.Core.Types.Item ->
                count: WorkTelegram.Core.Types.PositiveInt ->
                message: WorkTelegram.Core.Types.TelegramMessage -> unit
        
        [<RequireQualifiedAccess>]
        module private Forms =
            
            [<RequireQualifiedAccess>]
            module Keyboard =
                
                val addRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val deleteRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val showLastRecords:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val forceInspireItems:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val refresh: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val withoutSerial:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithOnlyName ->
                    Elmish.Keyboard
                
                val withoutMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithSerial ->
                    Elmish.Keyboard
                
                val withoutLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.Item ->
                    count: WorkTelegram.Core.Types.PositiveInt ->
                    Elmish.Keyboard
                
                val enterDeletionItemRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: WorkTelegram.Core.Types.DeletionItem ->
                    Elmish.Keyboard
                
                val hideDeletionItem:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.DeletionItem ->
                    Elmish.Keyboard
                
                val showDeletionItem:
                  ctx: ViewContext<'a,'b> ->
                    item: WorkTelegram.Core.Types.DeletionItem ->
                    Elmish.Keyboard
                
                val renderOffice:
                  office: WorkTelegram.Core.Types.Office ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    Elmish.Keyboard
                
                val accept:
                  onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    Elmish.Keyboard
                
                val noAuthManager: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val noAuthEmployer: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val deAuthEmployer:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    Elmish.Keyboard
                
                val authEmployer:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    Elmish.Keyboard
                
                val delegateEmployerChooseOffice:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    choosenOffice: WorkTelegram.Core.Types.Office ->
                    office: 'b -> Elmish.Keyboard
                
                val delegateEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val delegateOffice:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    manager: WorkTelegram.Core.Types.Manager ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuAuthEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuDeAuthEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuEmployerOperations:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuDelegateEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuOfficesOperations:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuGetExcelTableOfActualItems:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuGetExcelTableOfAllItems:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuAddEditItemRecord:
                  ctx: ViewContext<'a,'b> ->
                    asEmployerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val managerMenuDeletionAllItemRecords:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuDelegateOffice:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.Keyboard
                
                val startMakeOfficeProcess:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    Elmish.Keyboard
                
                val createOffice:
                  ctx: ViewContext<'a,'b> ->
                    recordOffice: WorkTelegram.Core.Types.Office ->
                    Elmish.Keyboard
            
            [<RequireQualifiedAccess>]
            module RenderView =
                
                val approvedEmployerMenu:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    employerState: EmployerProcess.EmployerContext ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val waitingApproveEmployerMenu:
                  ctx: ViewContext<'a,'b> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessEnteringSerial:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithOnlyName ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessEnteringMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithSerial ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessEnteringLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.Item ->
                    count: WorkTelegram.Core.Types.PositiveInt ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: WorkTelegram.Core.Types.DeletionItem ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val editDeletionItems:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    employerState: EmployerProcess.EmployerContext ->
                    items: WorkTelegram.Core.Types.DeletionItem seq ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val showRecords:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    items: 'c seq ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val renderOffices:
                  ctx: ViewContext<'a,'b> ->
                    offices: WorkTelegram.Infrastructure.OfficesMap ->
                    onClick: (WorkTelegram.Core.Types.Office ->
                                Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val employerAuthAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val managerAuthAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    manager: WorkTelegram.Core.Types.Manager ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val noAuth:
                  ctx: ViewContext<'a,'b> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val deAuthEmployers:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employers: WorkTelegram.Infrastructure.EmployersMap ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val authEmployers:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employers: WorkTelegram.Infrastructure.EmployersMap ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delegateOffice:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    managers: WorkTelegram.Infrastructure.ManagersMap ->
                    office: WorkTelegram.Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delegateEmployerChooseOffice:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: WorkTelegram.Core.Types.Employer ->
                    offices: WorkTelegram.Infrastructure.OfficesMap ->
                    office: 'b ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delegateEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employers: WorkTelegram.Infrastructure.EmployersMap ->
                    office: WorkTelegram.Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val employerOperations:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val managerOfficeOperations:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val managerMenuInOffice:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: WorkTelegram.Core.Types.Office ->
                    asEmployerState: EmployerProcess.EmployerContext ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val makeOfficeForStartWork:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val finishMakeOffice:
                  ctx: ViewContext<'a,'b> ->
                    office: WorkTelegram.Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val coreModelCatchError:
                  ctx: ViewContext<'a,'b> ->
                    message: 'c ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
        
        [<RequireQualifiedAccess>]
        module private ViewEmployer =
            
            val waitChoice:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                employerState: EmployerProcess.EmployerContext ->
                Elmish.RenderView
            
            val editDeletionItems:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                employerState: EmployerProcess.EmployerContext ->
                Elmish.RenderView
            
            val showLastDeletionItems:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                employerState: EmployerProcess.EmployerContext ->
                Elmish.RenderView
            
            [<RequireQualifiedAccess>]
            module DeletionProcess =
                
                val enteringName:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.RenderView
                
                val enteringSerial:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithOnlyName ->
                    Elmish.RenderView
                
                val enteringMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.ItemWithSerial ->
                    Elmish.RenderView
                
                val enteringCount:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.Item -> Elmish.RenderView
                
                val enteringLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: WorkTelegram.Core.Types.Item ->
                    count: WorkTelegram.Core.Types.PositiveInt ->
                    Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: WorkTelegram.Core.Types.DeletionItem ->
                    Elmish.RenderView
            
            [<RequireQualifiedAccess>]
            module AuthProcess =
                
                val enteringOffice:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    Elmish.RenderView
                
                val enteringLastFirstName:
                  ctx: ViewContext<'a,'b> ->
                    office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerRecord: WorkTelegram.Core.Types.Employer ->
                    Elmish.RenderView
            
            val deletionProcess:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                delProcess: EmployerProcess.Deletion -> Elmish.RenderView
            
            val authEmployer:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                employerAuth: AuthProcess.AuthEmployer -> Elmish.RenderView
        
        [<RequireQualifiedAccess>]
        module private ViewManager =
            
            [<RequireQualifiedAccess>]
            module AuthProcess =
                
                val enteringLastFirstName:
                  ctx: ViewContext<'a,'b> -> Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    manager: WorkTelegram.Core.Types.Manager ->
                    Elmish.RenderView
            
            [<RequireQualifiedAccess>]
            module MakeOffice =
                
                val enteringName:
                  ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                    managerState: ManagerProcess.ManagerContext ->
                    Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    recordOffice: WorkTelegram.Core.Types.Office ->
                    Elmish.RenderView
            
            val makeOfficeProcess:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                makeProcess: ManagerProcess.MakeOffice -> Elmish.RenderView
            
            val deAuthEmployers:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val authEmployers:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val delegateOffice:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val delegateEmployer:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val delegateEmployerChooseOffice:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office ->
                employer: WorkTelegram.Core.Types.Employer -> Elmish.RenderView
            
            val employerOperations:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val officeOperations:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val inOffice:
              ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
                managerState: ManagerProcess.ManagerContext ->
                office: WorkTelegram.Core.Types.Office -> Elmish.RenderView
            
            val noOffices:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
            
            val chooseOffice:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext ->
                offices: WorkTelegram.Infrastructure.OfficesMap ->
                Elmish.RenderView
            
            val authManager:
              ctx: ViewContext<'a,'b> ->
                managerAuth: AuthProcess.AuthManager -> Elmish.RenderView
        
        val private employerProcess:
          ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
            employerState: EmployerProcess.EmployerContext -> Elmish.RenderView
        
        val private managerProcess:
          ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
            managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
        
        val private authProcess:
          ctx: ViewContext<'a,WorkTelegram.Infrastructure.CacheCommand> ->
            authModel: AuthProcess.AuthModel -> Elmish.RenderView
        
        val view:
          env: WorkTelegram.Infrastructure.IAppEnv<'a,
                                                   WorkTelegram.Infrastructure.CacheCommand> ->
            dispatch: (UpdateMessage -> unit) ->
            model: Model.ModelContext<'b> -> Elmish.RenderView

