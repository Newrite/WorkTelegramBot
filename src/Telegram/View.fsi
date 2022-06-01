namespace WorkTelegram.Telegram
    
    module View =
        
        [<NoEquality; NoComparison>]
        type private ViewContext =
            {
              Dispatch: UpdateMessage -> unit
              BackCancelKeyboard: Elmish.Keyboard
              Env: Core.Types.Env
            }
        
        val private employerProcess:
          ctx: ViewContext -> employerState: EmployerProcess.EmployerContext
            -> Elmish.RenderView
        
        val private authProcess:
          ctx: ViewContext -> authModel: AuthProcess.Model -> Elmish.RenderView
        
        val view:
          env: Core.Types.Env -> history: System.Collections.Generic.Stack<'a>
          -> dispatch: (UpdateMessage -> unit) -> model: CoreModel
            -> Elmish.RenderView

