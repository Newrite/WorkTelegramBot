namespace WorkTelegram.Telegram
    
    module View =
        
        exception private ViewUnmatchedException of string
        
        [<NoEquality; NoComparison>]
        type private ViewContext<'CacheCommand,'ElmishCommand> =
            {
              Dispatch: (UpdateMessage -> unit)
              BackCancelKeyboard: Elmish.Keyboard
              AppEnv:
                Infrastructure.AppEnv.IAppEnv<'CacheCommand,'ElmishCommand>
            }
        
        module private Functions =
            
            val createExcelTableFromItemsAsBytes:
              items: seq<Core.Types.DeletionItem> -> byte[]
            
            val sendExcelItemsDocumentExn:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext ->
                items: seq<Core.Types.DeletionItem> ->
                officeId: Core.UMX.OfficeId -> unit
            
            val enteringLastFirstNameEmployerMessageHandle:
              ctx: ViewContext<'a,'b> ->
                office: Core.Types.Office ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringOfficeNameMessageHandle:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                managerState: ManagerProcess.ManagerContext ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringLastFirstNameManagerMessageHandle:
              ctx: ViewContext<'a,'b> ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringNameMessageHandle:
              employerState: EmployerProcess.EmployerContext ->
                ctx: ViewContext<'a,'b> ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringSerialMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: Core.Types.ItemWithOnlyName ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringMacAddressMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: Core.Types.ItemWithSerial ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringCountMessageHandleExn:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: Core.Types.Item ->
                message: Core.Types.TelegramMessage -> unit
            
            val enteringLocationMessageHandle:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                item: Core.Types.Item ->
                count: Core.Types.PositiveInt ->
                message: Core.Types.TelegramMessage -> unit
        
        module private Forms =
            
            module Keyboard =
                
                val addRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val deleteRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val refresh: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val withoutSerial:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.ItemWithOnlyName -> Elmish.Keyboard
                
                val withoutMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.ItemWithSerial -> Elmish.Keyboard
                
                val withoutLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.Item ->
                    count: Core.Types.PositiveInt -> Elmish.Keyboard
                
                val enterDeletionItemRecord:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: Core.Types.DeletionItem ->
                    Elmish.Keyboard
                
                val hideDeletionItem:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.DeletionItem -> Elmish.Keyboard
                
                val renderOffice:
                  office: Core.Types.Office ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    Elmish.Keyboard
                
                val accept:
                  onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    Elmish.Keyboard
                
                val noAuthManager: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val noAuthEmployer: ctx: ViewContext<'a,'b> -> Elmish.Keyboard
                
                val deAuthEmployer:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: Core.Types.Employer -> Elmish.Keyboard
                
                val authEmployer:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employer: Core.Types.Employer -> Elmish.Keyboard
                
                val managerMenuAuthEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuDeAuthEmployer:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuOfficesOperations:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuGetExcelTableOfActualItems:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: Core.Types.Office -> Elmish.Keyboard
                
                val managerMenuAddEditItemRecord:
                  ctx: ViewContext<'a,'b> ->
                    asEmployerState: EmployerProcess.EmployerContext ->
                    Elmish.Keyboard
                
                val managerMenuDeletionAllItemRecords:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    office: Core.Types.Office -> Elmish.Keyboard
                
                val startMakeOfficeProcess:
                  ctx: ViewContext<'a,'b> ->
                    managerState: ManagerProcess.ManagerContext ->
                    Elmish.Keyboard
                
                val createOffice:
                  ctx: ViewContext<'a,'b> ->
                    recordOffice: Core.Types.Office -> Elmish.Keyboard
            
            module RenderView =
                
                val approvedEmployerMenu:
                  ctx: ViewContext<'a,'b> ->
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
                    item: Core.Types.ItemWithOnlyName ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessEnteringMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.ItemWithSerial ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessEnteringLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.Item ->
                    count: Core.Types.PositiveInt ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val delProcessAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: Core.Types.DeletionItem ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val editDeletionItems:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    employerState: EmployerProcess.EmployerContext ->
                    items: seq<Core.Types.DeletionItem> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val renderOffices:
                  ctx: ViewContext<'a,'b> ->
                    offices: seq<Core.Types.Office> ->
                    onClick: (Core.Types.Office ->
                                Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val employerAuthAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employer: Core.Types.Employer ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val managerAuthAskingFinish:
                  ctx: ViewContext<'a,'b> ->
                    manager: Core.Types.Manager ->
                    onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val noAuth:
                  ctx: ViewContext<'a,'b> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val deAuthEmployers:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employers: seq<Core.Types.Employer> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val authEmployers:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    employers: seq<Core.Types.Employer> ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val managerMenuInOffice:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    office: Core.Types.Office ->
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
                    office: Core.Types.Office ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
                
                val coreModelCatchError:
                  ctx: ViewContext<'a,'b> ->
                    message: 'c ->
                    ((Funogram.Telegram.Types.Message -> unit) list ->
                       Elmish.RenderView)
        
        module private ViewEmployer =
            
            val waitChoice:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                employerState: EmployerProcess.EmployerContext ->
                Elmish.RenderView
            
            val editDeletionItems:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                employerState: EmployerProcess.EmployerContext ->
                Elmish.RenderView
            
            module DeletionProcess =
                
                val enteringName:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    Elmish.RenderView
                
                val enteringSerial:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.ItemWithOnlyName -> Elmish.RenderView
                
                val enteringMacAddress:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.ItemWithSerial -> Elmish.RenderView
                
                val enteringCount:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.Item -> Elmish.RenderView
                
                val enteringLocation:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    item: Core.Types.Item ->
                    count: Core.Types.PositiveInt -> Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerState: EmployerProcess.EmployerContext ->
                    recordedDeletionItem: Core.Types.DeletionItem ->
                    Elmish.RenderView
            
            module AuthProcess =
                
                val enteringOffice:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    Elmish.RenderView
                
                val enteringLastFirstName:
                  ctx: ViewContext<'a,'b> ->
                    office: Core.Types.Office -> Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    employerRecord: Core.Types.Employer -> Elmish.RenderView
            
            val deletionProcess:
              ctx: ViewContext<'a,'b> ->
                employerState: EmployerProcess.EmployerContext ->
                delProcess: EmployerProcess.Deletion -> Elmish.RenderView
            
            val authEmployer:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                employerAuth: AuthProcess.AuthEmployer -> Elmish.RenderView
        
        module private ViewManager =
            
            module AuthProcess =
                
                val enteringLastFirstName:
                  ctx: ViewContext<'a,'b> -> Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    manager: Core.Types.Manager -> Elmish.RenderView
            
            module MakeOffice =
                
                val enteringName:
                  ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                    managerState: ManagerProcess.ManagerContext ->
                    Elmish.RenderView
                
                val askingFinish:
                  ctx: ViewContext<'a,'b> ->
                    recordOffice: Core.Types.Office -> Elmish.RenderView
            
            val makeOfficeProcess:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                managerState: ManagerProcess.ManagerContext ->
                makeProcess: ManagerProcess.MakeOffice -> Elmish.RenderView
            
            val deAuthEmployers:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
            
            val authEmployers:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
            
            val inOffice:
              ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
                managerState: ManagerProcess.ManagerContext ->
                office: Core.Types.Office -> Elmish.RenderView
            
            val noOffices:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
            
            val chooseOffice:
              ctx: ViewContext<'a,'b> ->
                managerState: ManagerProcess.ManagerContext ->
                offices: seq<Core.Types.Office> -> Elmish.RenderView
            
            val authManager:
              ctx: ViewContext<'a,'b> ->
                managerAuth: AuthProcess.AuthManager -> Elmish.RenderView
        
        val private employerProcess:
          ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
            employerState: EmployerProcess.EmployerContext -> Elmish.RenderView
        
        val private managerProcess:
          ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
            managerState: ManagerProcess.ManagerContext -> Elmish.RenderView
        
        val private authProcess:
          ctx: ViewContext<Infrastructure.CacheCommand,'a> ->
            authModel: AuthProcess.AuthModel -> Elmish.RenderView
        
        val view:
          env: Infrastructure.AppEnv.IAppEnv<Infrastructure.CacheCommand,'a> ->
            dispatch: (UpdateMessage -> unit) ->
            model: Model.ModelContext<'b> -> Elmish.RenderView

