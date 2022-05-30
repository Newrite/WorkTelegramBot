namespace WorkTelegram.Telegram

open Funogram.Telegram.Types
open Funogram.Telegram.Bot
open System.Collections.Concurrent
open Funogram.Types
open System

module Elmish =

  let (|Message|Callback|CallbackWithMessage|NoMessageOrCallback|) (ctx: UpdateContext) =
    let msg = ctx.Update.Message
    let cq  = ctx.Update.CallbackQuery
    if cq.IsSome && cq.Value.Data.IsSome then
      CallbackWithMessage (cq.Value, cq.Value.Data.Value, cq.Value.Message.Value)
    elif msg.IsSome then
      Message msg.Value
    elif cq.IsSome && cq.Value.Data.IsSome && cq.Value.Message.IsSome then
      Callback (cq.Value, cq.Value.Data.Value)
    else
      NoMessageOrCallback

  [<NoComparison>]
  [<NoEquality>]
  type Button =
    { OnClick: UpdateContext -> unit
      Button: InlineKeyboardButton }

  [<RequireQualifiedAccess>]
  module Button =
    
    let create buttonText onClick =
      { OnClick = onClick
        Button  =
          { Text                         = buttonText 
            Url                          = None
            LoginUrl                     = None
            CallbackData                 = Guid.NewGuid() |> string |> Some
            SwitchInlineQuery            = None
            SwitchInlineQueryCurrentChat = None
            CallbackGame                 = None
            Pay                          = None } }

  [<NoComparison>]
  type Keyboard =
    { Buttons: seq<Button> }

  [<RequireQualifiedAccess>]
  module Keyboard =
    
    let create buttonList = { Buttons = Seq.ofList buttonList }

    let createSingle buttonText onClick =
      { Buttons = seq { Button.create buttonText onClick } }

  [<NoComparison>]
  type RenderView = 
    { MessageText:     string
      Keyboards:       seq<Keyboard>
      MessageHandlers: seq<Message -> unit> }

  [<RequireQualifiedAccess>]
  module RenderView =
    
    let create messageText keyboardsList functionsList =
      { MessageText     = messageText
        Keyboards       = Seq.ofList keyboardsList
        MessageHandlers = Seq.ofList functionsList }

  [<NoComparison>]
  [<NoEquality>]
  type ProcessorConfig =
    { Config:  BotConfig
      Message: Message }

  [<NoComparison>]
  [<NoEquality>]
  [<RequireQualifiedAccess>]
  type ProcessorCommands<'message> =
    | Update      of 'message
    | GetDispatch of AsyncReplyChannel<'message -> unit>
    | Message     of UpdateContext
    | Finish

  [<RequireQualifiedAccess>]
  [<NoComparison>]
  [<NoEquality>]
  type private MessageHandlerCommands =
    | UpdateRenderView of RenderView
    | Message          of UpdateContext
    | Finish

  type Dispatch<'message> = 'message -> unit
  type CallInit<'model>   = unit     -> 'model
  type CallGetChatState   = unit     -> Message list
  type CallSaveChatState  = Message  -> unit
  type CallDelChatState   = Message  -> unit

  [<NoComparison>]
  [<NoEquality>]
  type Program<'model, 'message> =
    private
     { Init:          Message   -> 'model
       Update:        'message  -> 'model -> CallInit<'model> -> 'model 
       View:          Dispatch<'message>  -> 'model           -> RenderView
       GetChatStates: CallGetChatState  option 
       SaveChatState: CallSaveChatState option
       DelChatState:  CallDelChatState  option }

  let modelViewUpdateProcessor 
    (processorConfig: ProcessorConfig) program (processor: MailboxProcessor<ProcessorCommands<_>>) =

    let render (renderView: RenderView) =

      let keyboardMarkup =
        { InlineKeyboard =
            renderView.Keyboards
            |> Seq.map (fun k ->
              k.Buttons
              |> Seq.map (fun b -> b.Button)) }

      Funogram.Telegram.Api.editMessageTextBase
        (Some (Int processorConfig.Message.Chat.Id)) (Some processorConfig.Message.MessageId) 
        None renderView.MessageText None None (Some keyboardMarkup)
      |> Funogram.Api.api processorConfig.Config
      |> Async.RunSynchronously
      |> ignore

    let dispatch msg = ProcessorCommands.Update msg |> processor.Post

    let modelInit      = program.Init processorConfig.Message
    let renderViewInit = program.View dispatch modelInit
    render renderViewInit

    let messageHandlerProcessor 
      renderViewInit (handler: MailboxProcessor<MessageHandlerCommands>) =

      let runOnClickIfMatch renderView data ctx =
        renderView.Keyboards
          |> Seq.iter (fun k ->
            k.Buttons
            |> Seq.iter (fun b ->
                if b.Button.CallbackData.IsSome 
                && b.Button.CallbackData.Value = data then
                  b.OnClick ctx))
      
      let rec cycle renderView = async {
        try
          let! msg = handler.Receive()
          match msg with
          | MessageHandlerCommands.UpdateRenderView rv ->
            return! cycle rv
          | MessageHandlerCommands.Message ctx ->
            match ctx with
            | Message message ->
              renderView.MessageHandlers
              |> Seq.iter (fun f -> f message)
            | NoMessageOrCallback -> ()
            | Callback (_, data) | CallbackWithMessage (_, data, _) ->
              runOnClickIfMatch renderView data ctx
            return! cycle renderView
          | MessageHandlerCommands.Finish ->
            (handler :> IDisposable).Dispose()
        with exn ->
          printfn $"Exception render: Msg {exn.Message}"
          return! cycle renderView
      }

      cycle renderViewInit

    let messageHandler = MailboxProcessor.Start(messageHandlerProcessor renderViewInit)

    let rec cycle model = async {
      let! msg = processor.Receive()
      match msg with
      | ProcessorCommands.Update message ->
        let newModel   = program.Update message model (fun () -> program.Init processorConfig.Message)
        let renderView = program.View dispatch newModel
        MessageHandlerCommands.UpdateRenderView renderView |> messageHandler.Post
        render renderView
        return! cycle newModel
      | ProcessorCommands.GetDispatch channel ->
        channel.Reply dispatch
        return! cycle model
      | ProcessorCommands.Message ctx ->
        MessageHandlerCommands.Message ctx |> messageHandler.Post
        return! cycle model
      | ProcessorCommands.Finish ->
        messageHandler.Post MessageHandlerCommands.Finish
        let chatId    = processorConfig.Message.Chat.Id
        let messageId = processorConfig.Message.MessageId
        Funogram.Telegram.Api.deleteMessage chatId messageId
        |> Funogram.Api.api processorConfig.Config
        |> Async.RunSynchronously
        |> ignore
        (processor :> IDisposable).Dispose()
    }
    
    cycle modelInit

  [<RequireQualifiedAccess>]
  module Program =
    
    let mkSimple view update init =
      { Init          = init
        Update        = update
        View          = view
        GetChatStates = None
        SaveChatState = None
        DelChatState  = None }

    let mkWithState view update init getState saveState delState =
      { Init          = init
        Update        = update
        View          = view
        GetChatStates = Some getState
        SaveChatState = Some saveState 
        DelChatState  = Some delState}

    let isWithStateFunctions program =
      program.GetChatStates.IsSome
      && program.SaveChatState.IsSome
      && program.DelChatState.IsSome

    let startProgram config onUpdate program =


      let dict = new ConcurrentDictionary<int64, MailboxProcessor<ProcessorCommands<_>>>()

      if isWithStateFunctions program then

        let messages = program.GetChatStates.Value()

        for message in messages do

          Funogram.Telegram.Api.deleteMessage message.Chat.Id message.MessageId
          |> Funogram.Api.api config
          |> Async.RunSynchronously
          |> ignore

          Funogram.Telegram.Api.sendMessage message.Chat.Id "Инициализация..."
          |> Funogram.Api.api config
          |> Async.RunSynchronously
          |> function
          | Ok msg ->
            let processorConfig = { Config = config; Message = msg }
            dict.TryAdd(
              msg.Chat.Id,
              MailboxProcessor.Start(modelViewUpdateProcessor processorConfig program)
            ) |> ignore
            if program.SaveChatState.IsSome then
              program.SaveChatState.Value msg
          | Error _ -> ()

      let internalUpdate update (ctx: Funogram.Telegram.Bot.UpdateContext) =
        match ctx with
        | Message m ->

          let IsStart =
            m.Text.IsSome 
            && (m.Text.Value = "/start") 
            && (dict.ContainsKey(m.Chat.Id) |> not)

          let startFunction() = 
            let sendedMessage =
              Funogram.Telegram.Api.sendMessage m.Chat.Id "Инициализация..."
              |> Funogram.Api.api config
              |> Async.RunSynchronously

            match sendedMessage with
            | Ok msg ->
              let processorConfig = { Config = config; Message = msg }
              dict.TryAdd(
                m.Chat.Id,
                MailboxProcessor.Start(modelViewUpdateProcessor processorConfig program)
              ) |> ignore
              if program.SaveChatState.IsSome then
                program.SaveChatState.Value msg
            | Error _ -> ()

          let IsFinish =
            m.Text.IsSome
            && (m.Text.Value = "/finish") 
            && dict.ContainsKey(m.Chat.Id)

          let finishFunction() =

            ProcessorCommands.Finish
            |> dict[m.Chat.Id].Post

            if program.DelChatState.IsSome then
              program.DelChatState.Value m

            dict.TryRemove(m.Chat.Id) |> ignore

          let IsRestart =
            m.Text.IsSome 
            && (m.Text.Value = "/restart") 
            && dict.ContainsKey(m.Chat.Id)

          let IsMessageAndActorInDict =
            m.Text.IsSome && dict.ContainsKey(m.Chat.Id)

          if IsStart then

            startFunction()

          if IsMessageAndActorInDict then
            ProcessorCommands.Message ctx
            |> dict[m.Chat.Id].Post

          if IsFinish then
            
            finishFunction()

          if IsRestart then
          
            finishFunction()
            startFunction()

        | CallbackWithMessage _ ->
          let chatId = ctx.Update.CallbackQuery.Value.Message.Value.Chat.Id
          if dict.ContainsKey(chatId) then
            ProcessorCommands.Message ctx
            |> dict[chatId].Post
        | Callback _
        | NoMessageOrCallback -> ()
        update ctx

      startBot config (internalUpdate onUpdate) None