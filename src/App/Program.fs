module Main

open WorkTelegram.Core
open WorkTelegram.Telegram
open WorkTelegram.Infrastructure

open Serilog
open System.Threading
open Funogram.Telegram.Bot

[<EntryPoint>]
let main _ =

  let tgToken = System.Environment.GetEnvironmentVariable("TelegramApiKey")

  let logger =
    LoggerConfiguration()
      .WriteTo.Console()
      .WriteTo.File("WorkTelegramBotLog.txt")
      .MinimumLevel.Verbose()
      .CreateLogger()

  let iLog =
    Logger.ILogBuilder
      logger.Information
      logger.Warning
      logger.Error
      logger.Fatal
      logger.Debug

  let databaseName = "WorkBotDatabase.sqlite3"

  let iDb =
    let conn = Database.createConnection iLog databaseName
    Database.IDbBuilder conn
    
  let iCache =
    let mailbox = MailboxProcessor.Start(Cache.cacheActor { Logger = iLog; Database = iDb })
    Cache.ICacheBuilder mailbox
  
  let iCfg = Configurer.IConfigurerBuilder { defaultConfig with Token = tgToken }
  
  let env = IAppEnvBuilder iLog.Logger iDb.Db iCache.Cache iCfg.Configurer

  Database.initalizationTables env

  // Wait for initialization cache
  Thread.Sleep(1000)

  let commands: Funogram.Telegram.Types.BotCommand array =
    [| { Command = "/start"
         Description = "Что бы начать взаимодействие с ботом выберите эту комманду" }
       { Command = "/restart"
         Description = "Что бы перезапустить работу с ботом выберите эту комманду" }
       { Command = "/finish"
         Description = "Что бы завершить взаимодействие с ботом выберите эту комманду" } |]

  Funogram.Telegram.Api.setMyCommands commands
  |> Funogram.Api.api env.Configurer.BotConfig
  |> Async.RunSynchronously
  |> function
    | Ok response ->
      if response then
        Logger.debug env "Succes set bot commands"
      else
        Logger.warning env "Set commands return false, maybe just don't update?"
    | Error err -> Logger.warning env $"Setting bot commands return ApiResponseError {err}"

  let botUpdate (ctx: UpdateContext) =
    if ctx.Update.Message.IsSome then
      let message = ctx.Update.Message.Value

      if message.Chat.Username <> ctx.Me.Username then
        Utils.deleteMessageBase env message |> ignore

  let history = System.Collections.Generic.Stack<_>()

  let rec appLoop (sleepTime: int) =

    Logger.info env $"Start application, sleep before start {sleepTime}"

    Thread.Sleep(sleepTime)

    try

      let getState = fun () -> Database.selectMessages env
      let setState = Database.insertMessage env
      let delState = Database.deleteMessageJson env
      let view = View.view env history
      let update = Update.update history
      let init = Init.init env history

      Elmish.Program.mkProgram env.Log view update init
      |> Elmish.Program.withState getState setState delState
      |> Elmish.Program.startProgram env.Config botUpdate
      |> Async.RunSynchronously

    with
    | exn ->

      history.Clear()

      let sleepTime =
        if sleepTime >= 60000 then
          60000
        else
          sleepTime * sleepTime

      Logger.fatal env
        $"App loop exception
          Message: {exn.Message}
          Stacktrace: {exn.StackTrace}
          New sleep time: {sleepTime}"

      appLoop sleepTime

  appLoop 1000

  0
