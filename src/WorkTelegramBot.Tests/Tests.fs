module Tests

open Xunit
open Swensen.Unquote

open WorkTelegram.Core
open WorkTelegram.Infrastructure

module Env =

  [<Interface>]
  type ITestEnv<'CacheCommand> =
    inherit ILog
    inherit ICache<'CacheCommand>
    inherit IDb

  let env () =

    let iTestEnvBuilder iLog iDb iCache =
      { new ITestEnv<_> with
          member _.Logger = iLog
          member _.Db = iDb
          member _.Cache = iCache }

    let iLog =
      Logger.ILogBuilder
        (printfn "INFO: %s")
        (printfn "WARN: %s")
        (printfn "ERRO: %s")
        (printfn "FATA: %s")
        (printfn "DEBU: %s")

    let databaseName = "WorkBotDatabaseTest.sqlite3"

    System.IO.File.Delete(databaseName)

    let iDb =
      let conn = Database.createConnection iLog databaseName
      Database.IDbBuilder conn

    let iCache =
      let mailbox =
        MailboxProcessor.Start(Cache.cacheActor { Logger = iLog; Database = iDb })

      Cache.ICacheBuilder mailbox

    iTestEnvBuilder iLog.Logger iDb.Db iCache.Cache

open FSharp.UMX

type WorkTelegramTests() =

  let env = Env.env ()

  let chatIds: ChatId list = [ % 1230L; % 192392139L; % 12300930L ]

  [<Fact>]
  let ``Chat id dto work`` () =

    let toDtoList =
      [ for id in chatIds do
          ChatIdDto.fromDomain id ]

    let fromDtoList =
      [ for dto in toDtoList do
          ChatIdDto.toDomain dto ]

    test <@ chatIds = fromDtoList @>

  [<Fact>]
  let ``Test init database schema table`` () = test <@ Database.initTables env = 0 @>
