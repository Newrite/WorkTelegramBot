module Main

open WorkTelegram.Core
open WorkTelegram.Telegram
open WorkTelegram.Infrastructure

open Serilog
open System.Threading
open Funogram.Types
open Funogram.Telegram.Bot

[<EntryPoint>]
let main _ =

  let TGToken = System.Environment.GetEnvironmentVariable("TelegramApiKey")

  let logger =
    (new LoggerConfiguration())
      .WriteTo.Console()
      .WriteTo.File("WorkTelegramBotLog.txt")
      .MinimumLevel.Verbose()
      .CreateLogger()

  let logging =
    { Debug   = logger.Debug
      Info    = logger.Information
      Error   = logger.Error
      Warning = logger.Warning
      Fatal   = logger.Fatal }

  let databaseName = "WorkBotDatabase.sqlite3"

  let env =
    { Log        = logging
      Config     = { defaultConfig with Token = TGToken }
      DBConn     = Database.createConnection logging databaseName
      CacheActor = MailboxProcessor.Start(Cache.cacheActor) }

  Database.initalizationTables env

  Cache.initialization env

  // Wait for initialization cache
  Thread.Sleep(1000)

  let commands: Funogram.Telegram.Types.BotCommand array =
    [| { Command = "/start";   Description = "Что бы начать взаимодействие с ботом выберите эту комманду"    }
       { Command = "/restart"; Description = "Что бы перезапустить работу с ботом выберите эту комманду"     }
       { Command = "/finish";  Description = "Что бы завершить взаимодействие с ботом выберите эту комманду" } |]

  Funogram.Telegram.Api.setMyCommands commands
  |> Funogram.Api.api env.Config
  |> Async.RunSynchronously
  |> function
  | Ok response ->
    if response then
      env.Log.Debug "Succes set bot commands"
    else
      env.Log.Warning "Set commands return false, maybe just don't update?"
  | Error err         ->
    env.Log.Warning $"Setting bot commands return ApiResponseError {err}"

  let botUpdate (ctx: UpdateContext) =
    if ctx.Update.Message.IsSome then
      let message = ctx.Update.Message.Value
      if message.Chat.Username <> ctx.Me.Username then
        Utils.deleteMessageBase env message |> ignore

  let history = System.Collections.Generic.Stack<_>()
  
  let rec appLoop (sleepTime: int) =

    env.Log.Info $"Start application, sleep before start {sleepTime}"

    Thread.Sleep(sleepTime)

    try

      let getState = fun () -> Database.selectMessages    env
      let setState =           Database.insertMessage     env
      let delState =           Database.deleteMessageJson env
      let view     =           View.view                  env history
      let update   =           Update.update                  history
      let init     =           Init.init                  env history

      Elmish.Program.mkWithState
        env.Log view update init getState setState delState
      |> Elmish.Program.startProgram env.Config botUpdate
      |> Async.RunSynchronously

    with exn ->

      let sleepTime = if sleepTime >= 60000 then 60000 else sleepTime * sleepTime

      env.Log.Fatal
        $"App loop exception
          Message: {exn.Message}
          Stacktrace: {exn.StackTrace}
          New sleep time: {sleepTime}"

      appLoop sleepTime

  appLoop 1000

  0

