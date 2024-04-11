namespace WorkTelegram.Telegram
    
    module Elmish =
        
        val (|Message|Callback|CallbackWithMessage|NoMessageOrCallback|) :
          ctx: Funogram.Telegram.Bot.UpdateContext ->
            Choice<Funogram.Telegram.Types.Message,
                   (Funogram.Telegram.Types.CallbackQuery * string),
                   (Funogram.Telegram.Types.CallbackQuery * string *
                    Funogram.Telegram.Types.Message),unit>
        
        [<NoComparison; NoEquality>]
        type Button =
            {
              OnClick: (Funogram.Telegram.Bot.UpdateContext -> unit)
              Button: Funogram.Telegram.Types.InlineKeyboardButton
            }
        
        [<RequireQualifiedAccess>]
        module Button =
            
            val create:
              buttonText: string ->
                onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) -> Button
        
        [<NoComparison; NoEquality>]
        type Keyboard =
            { Buttons: Button array }
        
        [<RequireQualifiedAccess>]
        module Keyboard =
            
            val create: buttonList: Button list -> Keyboard
            
            val createSingle:
              buttonText: string ->
                onClick: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                Keyboard
        
        [<NoComparison; NoEquality>]
        type RenderView =
            {
              MessageText: string
              Keyboards: Keyboard array
              MessageHandlers: (Funogram.Telegram.Types.Message -> unit) array
            }
        
        [<RequireQualifiedAccess>]
        module RenderView =
            
            val create:
              messageText: string ->
                keyboardsList: Keyboard list ->
                functionsList: (Funogram.Telegram.Types.Message -> unit) list ->
                RenderView
        
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
        
        type ElmishProcessorDict<'Message> =
            System.Collections.Concurrent.ConcurrentDictionary<int64,
                                                               Agent<ProcessorCommands<'Message>>>
        
        [<RequireQualifiedAccess; NoComparison; NoEquality>]
        type private MessageHandlerCommands =
            | UpdateRenderView of RenderView
            | Message of Funogram.Telegram.Bot.UpdateContext
            | Finish
        
        type Dispatch<'Message> = 'Message -> unit
        
        type CallInit<'Model> = unit -> 'Model
        
        type CallGetChatState = unit -> WorkTelegram.Infrastructure.MessagesMap
        
        type CallSaveChatState = Funogram.Telegram.Types.Message -> unit
        
        type CallDelChatState = Funogram.Telegram.Types.Message -> unit
        
        [<NoComparison; NoEquality>]
        type Program<'Model,'Message,'CacheCommand,'ElmishCommand> =
            private
              {
                Init: (Funogram.Telegram.Types.Message -> 'Model)
                Update: ('Message -> 'Model -> CallInit<'Model> -> 'Model)
                View: (Dispatch<'Message> -> 'Model -> RenderView)
                AppEnv:
                  WorkTelegram.Infrastructure.IAppEnv<'ElmishCommand,
                                                      'CacheCommand>
                GetChatStates: CallGetChatState option
                SaveChatState: CallSaveChatState option
                DelChatState: CallDelChatState option
              }
        
        val modelViewUpdateProcessor:
          processorConfig: ProcessorConfig ->
            program: Program<'a,'b,'c,'d> ->
            processor: Agent<ProcessorCommands<'b>> ->
            (ProcessorCommands<'b> -> System.Threading.Tasks.Task<unit>)
        
        [<RequireQualifiedAccess>]
        module Program =
            
            val mkProgram:
              env: WorkTelegram.Infrastructure.IAppEnv<'a,'b> ->
                view: (Dispatch<'c> -> 'd -> RenderView) ->
                update: ('c -> 'd -> CallInit<'d> -> 'd) ->
                init: (Funogram.Telegram.Types.Message -> 'd) ->
                Program<'d,'c,'b,'a>
            
            val withState:
              getState: CallGetChatState ->
                saveState: CallSaveChatState ->
                delState: CallDelChatState ->
                program: Program<'a,'b,'c,'d> -> Program<'a,'b,'c,'d>
            
            val isWithStateFunctions: program: Program<'a,'b,'c,'d> -> bool
            
            val startProgram:
              onUpdate: (Funogram.Telegram.Bot.UpdateContext -> unit) ->
                program: Program<'a,'b,'c,ProcessorCommands<'b>> -> Async<unit>

