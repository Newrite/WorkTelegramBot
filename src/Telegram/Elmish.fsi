namespace WorkTelegram.Telegram
    
    module Elmish =
        
        val (|Message|Callback|CallbackWithMessage|NoMessageOrCallback|) :
          ctx: Funogram.Telegram.Bot.UpdateContext
            -> Choice<Funogram.Telegram.Types.Message,
                      (Funogram.Telegram.Types.CallbackQuery * string),
                      (Funogram.Telegram.Types.CallbackQuery * string *
                       Funogram.Telegram.Types.Message),unit>
        
        [<NoComparison; NoEquality>]
        type Button =
            {
              OnClick: Funogram.Telegram.Bot.UpdateContext -> unit
              Button: Funogram.Telegram.Types.InlineKeyboardButton
            }
        
        module Button =
            
            val create:
              buttonText: string
              -> onClick: (Funogram.Telegram.Bot.UpdateContext -> unit)
                -> Button
        
        [<NoComparison>]
        type Keyboard =
            { Buttons: seq<Button> }
        
        module Keyboard =
            
            val create: buttonList: Button list -> Keyboard
            
            val createSingle:
              buttonText: string
              -> onClick: (Funogram.Telegram.Bot.UpdateContext -> unit)
                -> Keyboard
        
        [<NoComparison>]
        type RenderView =
            {
              MessageText: string
              Keyboards: seq<Keyboard>
              MessageHandlers: seq<(Funogram.Telegram.Types.Message -> unit)>
            }
        
        module RenderView =
            
            val create:
              messageText: string -> keyboardsList: Keyboard list
              -> functionsList: (Funogram.Telegram.Types.Message -> unit) list
                -> RenderView
        
        [<NoComparison; NoEquality>]
        type ProcessorConfig =
            {
              Config: Funogram.Types.BotConfig
              Message: Funogram.Telegram.Types.Message
            }
        
        [<NoComparison; NoEquality; RequireQualifiedAccess>]
        type ProcessorCommands<'Message> =
            | Update of 'Message
            | GetDispatch of AsyncReplyChannel<('Message -> unit)>
            | Message of Funogram.Telegram.Bot.UpdateContext
            | Finish
        
        [<RequireQualifiedAccess; NoComparison; NoEquality>]
        type private MessageHandlerCommands =
            | UpdateRenderView of RenderView
            | Message of Funogram.Telegram.Bot.UpdateContext
            | Finish
        
        type Dispatch<'Message> = 'Message -> unit
        
        type CallInit<'Model> = unit -> 'Model
        
        type CallGetChatState = unit -> Funogram.Telegram.Types.Message list
        
        type CallSaveChatState = Funogram.Telegram.Types.Message -> unit
        
        type CallDelChatState = Funogram.Telegram.Types.Message -> unit
        
        [<NoComparison; NoEquality>]
        type Program<'Model,'Message> =
            private {
                      Init: Funogram.Telegram.Types.Message -> 'Model
                      Update: 'Message -> 'Model -> CallInit<'Model> -> 'Model
                      View: Dispatch<'Message> -> 'Model -> RenderView
                      Log: Infrastructure.AppEnv.ILog
                      GetChatStates: CallGetChatState option
                      SaveChatState: CallSaveChatState option
                      DelChatState: CallDelChatState option
                    }
        
        val modelViewUpdateProcessor:
          processorConfig: ProcessorConfig -> program: Program<'a,'b>
          -> processor: MailboxProcessor<ProcessorCommands<'b>> -> Async<unit>
        
        module Program =
            
            val mkProgram:
              logger: Infrastructure.AppEnv.ILog
              -> view: (Dispatch<'a> -> 'b -> RenderView)
              -> update: ('a -> 'b -> CallInit<'b> -> 'b)
              -> init: (Funogram.Telegram.Types.Message -> 'b) -> Program<'b,'a>
            
            val withState:
              getState: CallGetChatState -> saveState: CallSaveChatState
              -> delState: CallDelChatState -> program: Program<'a,'b>
                -> Program<'a,'b>
            
            val isWithStateFunctions: program: Program<'a,'b> -> bool
            
            val startProgram:
              config: Funogram.Types.BotConfig
              -> onUpdate: (Funogram.Telegram.Bot.UpdateContext -> unit)
              -> program: Program<'a,'b> -> Async<unit>

