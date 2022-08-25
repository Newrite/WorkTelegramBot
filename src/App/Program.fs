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

  let tgToken = System.Environment.GetEnvironmentVariable("TelegramApiKey")

  let logger =
    LoggerConfiguration()
      .WriteTo.Console()
      .WriteTo.File("WorkTelegramBotLog.txt")
      .MinimumLevel.Verbose()
      .CreateLogger()

  let iLog =
    Logger.ILogBuilder logger.Information logger.Warning logger.Error logger.Fatal logger.Debug

  let databaseName = "WorkBotDatabase.sqlite3"

  let iDb =
    let conn = Database.createConnection iLog databaseName
    Database.IDbBuilder conn

  Database.initTables iDb iLog |> ignore

  let iCache =
    let agent = Agent.MakeAndStartDefault(Cache.cacheActor { Logger = iLog; Database = iDb })

    Cache.ICacheBuilder agent
    
  let mutable cts = new CancellationTokenSource()
    
  let onError (exn: Exception) =
    Logger.error iLog
      $"On Error handler
        Message: {exn.Message}
        Cancel async token"
    cts.Cancel()
    
  let iCfg = Configurer.IConfigurerBuilder { Config.defaultConfig with Token = tgToken; OnError = onError } (Elmish.ElmishProcessorDict<_>())

  let env = IAppEnvBuilder iLog.Logger iDb.Db iCache.Cache iCfg.Configurer

  let commands: Funogram.Telegram.Types.BotCommand array =
    [| { Command = "/start"
         Description = "Что бы начать взаимодействие с ботом либо перезапустить его выберите эту комманду" }
       { Command = "/finish"
         Description = "Что бы завершить взаимодействие с ботом выберите эту комманду" } |]

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

      let getState = fun () -> Cache.getTelegramMessages env

      let setState message =
        Cache.tryAddOrUpdateTelegramMessage env message
        |> ignore

      let delState (message: TelegramMessage) =
        Cache.tryDeleteTelegramMessage env %message.Chat.Id
        |> ignore

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
