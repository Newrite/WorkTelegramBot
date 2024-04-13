namespace WorkTelegram.Telegram

open WorkTelegram.Infrastructure

open Funogram.Telegram.Types
open Funogram.Telegram.Bot
open System.Collections.Concurrent
open Funogram.Types
open System
open FSharp.UMX

module Elmish =

  let (|Message|Callback|CallbackWithMessage|NoMessageOrCallback|) (ctx: UpdateContext) =
    let msg = ctx.Update.Message
    let cq = ctx.Update.CallbackQuery

    if cq.IsSome && cq.Value.Data.IsSome then
      CallbackWithMessage(cq.Value, cq.Value.Data.Value, cq.Value.Message.Value)
    elif msg.IsSome then
      Message msg.Value
    elif cq.IsSome
         && cq.Value.Data.IsSome
         && cq.Value.Message.IsSome then
      Callback(cq.Value, cq.Value.Data.Value)
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
        Button =
          { Text = buttonText
            Url = None
            LoginUrl = None
            CallbackData = Guid.NewGuid() |> string |> Some
            WebApp = None
            SwitchInlineQuery = None
            SwitchInlineQueryCurrentChat = None
            CallbackGame = None
            Pay = None } }

  [<NoComparison>]
  [<NoEquality>]
  type Keyboard = { Buttons: Button[] }

  [<RequireQualifiedAccess>]
  module Keyboard =

    let create buttonList = { Buttons = Array.ofList buttonList }

    let createSingle buttonText onClick = create [ Button.create buttonText onClick ]

  [<NoComparison>]
  [<NoEquality>]
  type RenderView =
    { MessageText: string
      Keyboards: Keyboard[]
      MessageHandlers: (Message -> unit)[] }

  [<RequireQualifiedAccess>]
  module RenderView =

    let create messageText keyboardsList functionsList =
      { MessageText = messageText
        Keyboards = Array.ofList keyboardsList
        MessageHandlers = Array.ofList functionsList }

  [<NoComparison>]
  [<NoEquality>]
  type ProcessorConfig = { Config: BotConfig; Message: Message }

  [<NoComparison>]
  [<NoEquality>]
  [<RequireQualifiedAccess>]
  type ProcessorCommands<'Message> =
    | Update of 'Message
    | GetDispatch of AsyncReplyChannel<'Message -> unit>
    | Message of UpdateContext
    | Finish

  type ElmishProcessorDict<'Message> = ConcurrentDictionary<int64, Agent<ProcessorCommands<'Message>>>

  [<RequireQualifiedAccess>]
  [<NoComparison>]
  [<NoEquality>]
  type private MessageHandlerCommands =
    | UpdateRenderView of RenderView
    | Message of UpdateContext
    | Finish

  type Dispatch<'Message> = 'Message -> unit
  type CallInit<'Model> = unit -> 'Model
  type CallGetChatState = unit -> MessagesMap
  type CallSaveChatState = Message -> unit
  type CallDelChatState = Message -> unit

  [<NoComparison>]
  [<NoEquality>]
  type Program<'Model, 'Message, 'CacheCommand, 'ElmishCommand> =
    private
      { Init: Message -> 'Model
        Update: 'Message -> 'Model -> CallInit<'Model> -> 'Model
        View: Dispatch<'Message> -> 'Model -> RenderView
        AppEnv: IAppEnv<'ElmishCommand, 'CacheCommand>
        GetChatStates: CallGetChatState option
        SaveChatState: CallSaveChatState option
        DelChatState: CallDelChatState option }

  let modelViewUpdateProcessor
    (processorConfig: ProcessorConfig)
    program
    (processor: Agent<ProcessorCommands<_>>)
    =

    let render (renderView: RenderView) =

      let keyboardMarkup =
        { InlineKeyboard =
            renderView.Keyboards
            |> Array.map (fun k -> k.Buttons |> Array.map (fun b -> b.Button)) }

      Funogram.Telegram.Req.EditMessageText.Make(
        renderView.MessageText, 
        Int processorConfig.Message.Chat.Id, 
        processorConfig.Message.MessageId, 
        replyMarkup = keyboardMarkup)
      |> Funogram.Api.api processorConfig.Config
      |> Async.RunSynchronously
      |> ignore

    let dispatch msg = ProcessorCommands.Update msg |> processor.Post

    let modelInit = program.Init processorConfig.Message
    let renderViewInit = program.View dispatch modelInit
    render renderViewInit

    let messageHandlerProcessor renderViewInit (handler: Agent<MessageHandlerCommands>) =

      let runOnClickIfMatch renderView data ctx =
        renderView.Keyboards
        |> Seq.iter (fun k ->
          k.Buttons
          |> Seq.iter (fun b ->
            if b.Button.CallbackData.IsSome
               && b.Button.CallbackData.Value = data then
              b.OnClick ctx))

      let mutable renderView = renderViewInit

      let cycle msg =
        task {
          try

            match msg with
            | MessageHandlerCommands.UpdateRenderView rv -> renderView <- rv
            | MessageHandlerCommands.Message ctx ->
              match ctx with
              | Message message ->
                renderView.MessageHandlers
                |> Seq.iter (fun f -> f message)
              | NoMessageOrCallback -> ()
              | Callback (_, data)
              | CallbackWithMessage (_, data, _) -> runOnClickIfMatch renderView data ctx

            | MessageHandlerCommands.Finish -> (handler :> IDisposable).Dispose()

          with
          | exn ->
            Logger.error program.AppEnv $"Raise exception in render actor, message {exn.Message}"
        }

      cycle

    let messageHandler = Agent.MakeAndStartInjected(messageHandlerProcessor renderViewInit)

    let mutable model = modelInit

    let rec cycle msg =
      task {
        try

          match msg with
          | ProcessorCommands.Update message ->
            let newModel =
              program.Update message model (fun () -> program.Init processorConfig.Message)

            let renderView = program.View dispatch newModel

            MessageHandlerCommands.UpdateRenderView renderView
            |> messageHandler.Post

            render renderView
            model <- newModel
          | ProcessorCommands.GetDispatch channel ->
            channel.Reply dispatch
          | ProcessorCommands.Message ctx ->
            MessageHandlerCommands.Message ctx
            |> messageHandler.Post

          | ProcessorCommands.Finish ->
            messageHandler.Post MessageHandlerCommands.Finish
            let chatId = processorConfig.Message.Chat.Id
            let messageId = processorConfig.Message.MessageId

            Funogram.Telegram.Api.deleteMessage chatId messageId
            |> Funogram.Api.api processorConfig.Config
            |> Async.RunSynchronously
            |> ignore

            (processor :> IDisposable).Dispose()
        with
        | exn ->
          Logger.error program.AppEnv $"Raise exception in elmish actor, message {exn.Message}"
      }

    cycle

  [<RequireQualifiedAccess>]
  module Program =

    let mkProgram env view update init =
      { Init = init
        Update = update
        View = view
        AppEnv = env
        GetChatStates = None
        SaveChatState = None
        DelChatState = None }

    let withState getState saveState delState program =
      { program with
          GetChatStates = Some getState
          SaveChatState = Some saveState
          DelChatState = Some delState }

    let isWithStateFunctions program =

      program.GetChatStates.IsSome
      && program.SaveChatState.IsSome
      && program.DelChatState.IsSome

    let startProgram onUpdate program =

      let dict = program.AppEnv.Configurer.ElmishDict
      let config = program.AppEnv.Configurer.BotConfig
      let eventsBus = program.AppEnv.Bus.Events
        
      if isWithStateFunctions program then

        let getStates = program.GetChatStates.Value
        let saveState = program.SaveChatState.Value

        let messages = getStates ()

        for message in messages do

          Funogram.Telegram.Req.EditMessageText.Make("Инициализация...", Int message.Value.Chat.Id, message.Value.MessageId)
          |> Funogram.Api.api config
          |> Async.RunSynchronously
          |> function
            | Ok editmsg -> 
              match editmsg with 
              | EditMessageResult.Message msg ->
                let processorConfig = { Config = config; Message = msg }

                dict.TryAdd(
                  msg.Chat.Id,
                  Agent.MakeAndStartInjected(modelViewUpdateProcessor processorConfig program)
                )
                |> ignore

                saveState msg
              | EditMessageResult.Success _ -> ()
            | Error _ -> ()

      let internalUpdate update (ctx: UpdateContext) =
        
        let startFunction (msg: Message) =
          let sendedMessage =
            Funogram.Telegram.Api.sendMessage msg.Chat.Id "Инициализация..."
            |> Funogram.Api.api config
            |> Async.RunSynchronously
          match sendedMessage with
          | Ok msg ->
            let processorConfig = { Config = config; Message = msg }
            dict.TryAdd(
              msg.Chat.Id,
              Agent.MakeAndStartInjected(modelViewUpdateProcessor processorConfig program)
            )
            |> ignore
            if program.SaveChatState.IsSome then
              program.SaveChatState.Value msg
          | Error _ -> ()
          
        let finishFunction (msg: Message) =
          if dict.ContainsKey(msg.Chat.Id) then
            ProcessorCommands.Finish |> dict[msg.Chat.Id].Post
            if program.DelChatState.IsSome then
              program.DelChatState.Value msg
            match dict.TryRemove(msg.Chat.Id) with
            | true, _ -> true
            | false, _ -> false
          else true
        
        let handleEvents msg =
          while eventsBus.Count > 0 do
            let event = eventsBus.Pop()
            match event with
            | EventBusMessage.RemoveFromElmishDict chatId ->
              match finishFunction msg with
              | true -> Logger.info program.AppEnv "Succes remove event for chat id %d" %chatId
              | false -> Logger.error program.AppEnv "Error when try remove event for chat id %d" %chatId
        
        match ctx with
        | Message m ->
          
          handleEvents m

          let isStart =
            m.Text.IsSome
            && (m.Text.Value = "/start")
            && (dict.ContainsKey(m.Chat.Id) |> not)

          let isFinish =
            m.Text.IsSome
            && (m.Text.Value = "/finish")
            && dict.ContainsKey(m.Chat.Id)

          let isRestart =
            m.Text.IsSome
            && (m.Text.Value = "/start")
            && dict.ContainsKey(m.Chat.Id)

          let isMessageAndActorInDict = m.Text.IsSome && dict.ContainsKey(m.Chat.Id)

          if isStart then

            startFunction m

          if isMessageAndActorInDict then
            ProcessorCommands.Message ctx
            |> dict[m.Chat.Id].Post

          if isFinish then

            finishFunction m |> ignore

          if isRestart then

            finishFunction m |> ignore
            startFunction m

        | CallbackWithMessage (_, _, m) ->
          let chatId = ctx.Update.CallbackQuery.Value.Message.Value.Chat.Id

          handleEvents m
          if dict.ContainsKey(chatId) then
            ProcessorCommands.Message ctx |> dict[chatId].Post
        | Callback _
        | NoMessageOrCallback -> ()

        update ctx

      startBot config (internalUpdate onUpdate) None
