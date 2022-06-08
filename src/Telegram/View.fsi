namespace WorkTelegram.Telegram
    
    module View =
        
        exception private ViewUnmatchedException of string
        
        [<NoEquality; NoComparison>]
        type private ViewContext<'Command> =
            {
              Dispatch: UpdateMessage -> unit
              BackCancelKeyboard: Elmish.Keyboard
              AppEnv: Infrastructure.AppEnv.IAppEnv<'Command>
            }
        
        val private employerProcess:
          ctx: ViewContext<Infrastructure.CacheCommand>
          -> employerState: EmployerProcess.EmployerContext -> Elmish.RenderView
        
        val private authProcess:
          ctx: ViewContext<Infrastructure.CacheCommand>
          -> authModel: AuthProcess.Model -> Elmish.RenderView
        
        val view:
          env: Infrastructure.AppEnv.IAppEnv<Infrastructure.CacheCommand>
          -> history: System.Collections.Generic.Stack<'a>
          -> dispatch: (UpdateMessage -> unit) -> model: CoreModel
            -> Elmish.RenderView

