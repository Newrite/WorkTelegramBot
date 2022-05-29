module Main

open WorkTelegram.Core
open WorkTelegram.Telegram
open WorkTelegram.Infrastructure

open Serilog
open System.Threading
open Funogram.Telegram.Bot
open Funogram.Types

[<EntryPoint>]
let main _ =

  let TGToken = System.Environment.GetEnvironmentVariable("TelegramApiKey")

  let logger =
    (new Serilog.LoggerConfiguration())
      .WriteTo.Console()
      .WriteTo.File("WorkTelegramBotLog.txt")
      .MinimumLevel.Verbose()
      .CreateLogger()


  let logging =
    { Debug   = logger.Debug
      Info    = logger.Information
      Error   = logger.Error
      Warning = logger.Warning }

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

  //Funogram.Telegram.Api.deleteMyCommands()
  //|> Funogram.Api.api env.Config
  //|> Async.RunSynchronously
  //|> ignore

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

  let update (ctx: UpdateContext) =
    if ctx.Update.Message.IsSome then
      let message = ctx.Update.Message.Value
      if message.Chat.Username <> ctx.Me.Username then
        Utils.deleteMessageBase env message |> ignore

  let history = System.Collections.Generic.Stack<_>()

  try

    Elmish.Program.mkSimple (View.view env history) (Update.update history) (Init.init env history)
    |> Elmish.Program.startProgram env.Config update
    |> Async.RunSynchronously

  with exn ->

    env.Log.Error $"Exception message {exn.Message}
      Trace: {exn.StackTrace}"

  0

