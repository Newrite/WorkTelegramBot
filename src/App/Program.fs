module Main

open System
open WorkTelegram.Core
open WorkTelegram.Telegram
open WorkTelegram.Infrastructure

open Serilog
open System.Threading
open Funogram.Telegram.Bot
open FSharp.UMX

[<EntryPoint>]
let main _ =

  let tgToken = Environment.GetEnvironmentVariable("TelegramApiKey")

  let logFile = "WorkTelegramBotLog.txt"

  let logger =
    LoggerConfiguration()
      .WriteTo.Console()
      .WriteTo.File(logFile)
      .MinimumLevel.Verbose()
      .CreateLogger()

  let iLog =
    Logger.ILogBuilder logger.Information logger.Warning logger.Error logger.Fatal logger.Debug

  let databaseName = "WorkBotDatabase.sqlite3"

  let iDb =
    let conn = Database.createConnection iLog databaseName
    Database.IDbBuilder conn

  Database.initTables iDb iLog |> ignore


  let iRep =
    
    let cache = Cache.initializationCache iLog iDb Repository.errorHandler

    let cacheAgent = Agent.MakeAndStartDefault(Cache.cacheAgent cache)


    Repository.IRepBuilder cacheAgent
    
  let mutable cts = new CancellationTokenSource()
    
  let onError (exn: Exception) =
    Logger.error iLog
      $"On Error handler
        Message: {exn.Message}
        Cancel async token"
    cts.Cancel()
    
  let iCfg = Configurer.IConfigurerBuilder { Config.defaultConfig with Token = tgToken; OnError = onError } (Elmish.ElmishProcessorDict<_>())
  
  let iBus = EventBus.IEventBus <| EventsStack() 

  let env = IAppEnvBuilder iLog.Logger iDb.Db iRep.Repository iCfg.Configurer iBus.Bus

  let commands: Funogram.Telegram.Types.BotCommand array =
    [| { Command = "/start"
         Description = "Что бы начать взаимодействие с ботом либо перезапустить его выберите эту комманду" }
       { Command = "/finish"
         Description = "Что бы завершить взаимодействие с ботом выберите эту комманду" }
       { Command = "/pin"
         Description = "Что бы бот отправил сообщение которое не будет удалено, выберите эту команду, позволит оставить бота в истории сообщений" } |]

  Funogram.Telegram.Req.SetMyCommands.Make commands
  |> Funogram.Api.api env.Configurer.BotConfig
  |> Async.RunSynchronously
  |> function
    | Ok response ->
      if response then
        Logger.debug env "Success set bot commands"
      else
        Logger.warning env "Set commands return false, maybe just don't update?"
    | Error err -> Logger.warning env $"Setting bot commands return ApiResponseError {err}"

  let botUpdate (ctx: UpdateContext) =
    if ctx.Update.Message.IsSome then
      let message = ctx.Update.Message.Value

      if message.Chat.Username <> ctx.Me.Username then
        Utils.deleteMessageBase env message |> ignore


  let rec appLoop (sleepTime: int) =

    Logger.info env $"Start application, sleep before start {sleepTime}"

    Thread.Sleep(sleepTime)

    try

      let getState = fun () -> Repository.messages env

      let setState (message: TelegramMessage) =
        let messages = getState()
        if messages.ContainsKey(%message.Chat.Id) then
          Repository.tryUpdateMessage env message |> ignore
        else
          Repository.tryAddMessage env message |> ignore

      let delState (message: TelegramMessage) =
        Repository.tryDeleteMessage env message |> ignore

      let view = View.view env
      let update = Update.update env
      let init = ModelContext<CoreModel>.Init env

      let asyncProgram =
        Elmish.Program.mkProgram env view update init
        |> Elmish.Program.withState getState setState delState
        |> Elmish.Program.startProgram botUpdate

      Async.RunSynchronously(asyncProgram, cancellationToken = cts.Token)

    with
    | exn ->

      let sleepTime =
        let st = sleepTime * 5
        if st >= 60000 then
          60000
        else
          st

      Logger.fatal
        env
        $"App loop exception
          Message: {exn.Message}
          Stacktrace: {exn.StackTrace}
          New sleep time: {sleepTime}"

      cts <- new CancellationTokenSource()
      Logger.info env "Change token for new app loop iterate"
      
      appLoop sleepTime

  appLoop 1000

  0
