module Main

open WorkTelegram.Core
open WorkTelegram.Logger
open WorkTelegram.Telegram
open WorkTelegram.Infrastructure

open Microsoft.Extensions.Logging
open System.Threading
open Funogram.Telegram.Bot
open Funogram.Types

type AdditionalData = {
    Name : string
}

[<EntryPoint>]
let main _ =

  let iChatId = 944079861L

  let TGToken = System.Environment.GetEnvironmentVariable("TelegramApiKey")

  let logger =

    let appName = "WorkingBotApp"
    
    LoggerFactory
      .Create(fun builder -> builder.AddConsole().SetMinimumLevel(LogLevel.Trace) |> ignore)
      .CreateLogger(appName)


  let logging =
    { Debug   = logger.LogDebug
      Info    = logger.LogInformation
      Error   = logger.LogError
      Warning = logger.LogWarning }

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
  |> ignore

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

