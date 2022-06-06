namespace WorkTelegram.Infrastructure

open WorkTelegram.Core
open System.Data
open FSharp.UMX
open WorkTelegram.Core.Field
open WorkTelegram.Core.Types

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type CacheCommand =
  | GetOffices of AsyncReplyChannel<Office list>
  | GetDeletionItems of AsyncReplyChannel<DeletionItem list>
  | GetEmployers of AsyncReplyChannel<Employer list>
  | GetManagers of AsyncReplyChannel<Manager list>
  | GetMessages of AsyncReplyChannel<Funogram.Telegram.Types.Message list>
  | GetOfficesByManagerId of UMX.ChatId * AsyncReplyChannel<Office list>
  | TryGetEmployerByChatId of UMX.ChatId * AsyncReplyChannel<Result<Employer, BusinessError>>
  | TryGetManagerByChatId of UMX.ChatId * AsyncReplyChannel<Result<Manager, BusinessError>>
  | TryGetMessageByChatId of
    UMX.ChatId *
    AsyncReplyChannel<Result<Funogram.Telegram.Types.Message, BusinessError>>
  | TryAddOfficeInDb of RecordOffice * AsyncReplyChannel<Result<Office, BusinessError>>
  | TryAddEmployerInDb of RecordEmployer * AsyncReplyChannel<Result<Employer, BusinessError>>
  | TryAddManagerInDb of ManagerDto * AsyncReplyChannel<Result<Manager, BusinessError>>
  | TryAddDeletionItemInDb of RecordDeletionItem * AsyncReplyChannel<Result<DeletionItem, AppError>>
  | TryAddOrUpdateMessageInDb of
    MessageDto *
    AsyncReplyChannel<Result<Funogram.Telegram.Types.Message, AppError>>
  | TryUpdateEmployerApprovedInDb of
    Employer *
    bool *
    AsyncReplyChannel<Result<Employer, BusinessError>>
  | TrySetDeletionOnItemsOfOffice of OfficeId * AsyncReplyChannel<Result<unit, BusinessError>>
  | TryHideDeletionItem of DeletionId * AsyncReplyChannel<Result<unit, BusinessError>>
  | TryDeleteOffice of OfficeId * AsyncReplyChannel<Result<unit, BusinessError>>

[<NoComparison>]
type CacheContext =
  { Conn: IDbConnection
    Logger: WorkTelegram.Infrastucture.AppEnv.IAppLogger }

[<NoComparison>]
type private Cache =
  { Employers: Employer list
    Offices: Office list
    Managers: Manager list
    DeletionItems: DeletionItem list
    Messages: Funogram.Telegram.Types.Message list }

module Cache =

  let private initializationCache (ctx: CacheContext) =

    let employersDto = Database.selectEmployers ctx.Conn
    let managersDto = Database.selectManagers ctx.Conn
    let officesDto = Database.selectOffices ctx.Conn
    let deletionItemsDto = Database.selectDeletionItems ctx.Conn
    let messagesDto = Database.selectMessages ctx.Conn

    let findManager (id: int64) =
      List.find (fun (m: ManagerDto) -> m.ChatId = id) managersDto

    let findOffice (id: int64) =
      List.find (fun (o: OfficeDto) -> o.OfficeId = id) officesDto

    let findEmployer (id: int64) =
      List.find (fun (e: EmployerDto) -> e.ChatId = id) employersDto

    let managers = List.map (fun m -> ManagerDto.toDomain m) managersDto

    let offices =
      List.map (fun o -> OfficeDto.toDomain o (findManager o.ManagerId)) officesDto

    let employers =
      List.map
        (fun (e: EmployerDto) ->
          let office = findOffice e.OfficeId
          let manager = findManager office.ManagerId
          EmployerDto.toDomain office manager e)
        employersDto

    let deletionItems =
      List.map
        (fun (di: DeletionItemDto) ->
          let office = findOffice di.OfficeId
          let manager = findManager office.ManagerId

          let employer =
            match List.tryFind (fun (m: ManagerDto) -> m.ChatId = di.ChatId) managersDto with
            | Some m ->
              { FirstName = m.FirstName
                LastName = m.LastName
                ChatId = m.ChatId
                IsApproved = true
                OfficeId = di.OfficeId }
            | None -> findEmployer di.ChatId

          DeletionItemDto.toDomain di employer office manager)
        deletionItemsDto

    let messages = List.map (fun (m: MessageDto) -> MessageDto.toDomain m) messagesDto

    let cache =
      { Employers = employers
        Managers = managers
        Offices = offices
        DeletionItems = deletionItems
        Messages = messages }

    cache

  let cacheActor (ctx: CacheContext) (inbox: MailboxProcessor<CacheCommand>) =

    let cache = initializationCache ctx

    let inline finder predicate collection (channel: AsyncReplyChannel<Result<'a, BusinessError>>) =
      let entity = List.tryFind predicate collection

      match entity with
      | Some e -> e |> Ok |> channel.Reply
      | None ->
        BusinessError.NotFoundInDatabase
        |> Error
        |> channel.Reply

    let rec cacheHandler cache =
      async {

        match! inbox.Receive() with
        | CacheCommand.GetOffices channel ->
          channel.Reply cache.Offices
          return! cacheHandler cache
        | CacheCommand.GetDeletionItems channel ->
          channel.Reply cache.DeletionItems
          return! cacheHandler cache
        | CacheCommand.GetEmployers channel ->
          channel.Reply cache.Employers
          return! cacheHandler cache
        | CacheCommand.GetManagers channel ->
          channel.Reply cache.Managers
          return! cacheHandler cache
        | CacheCommand.GetMessages channel ->
          channel.Reply cache.Messages
          return! cacheHandler cache
        | CacheCommand.GetOfficesByManagerId (chatId, channel) ->
          List.tryFind (fun o -> o.Manager.ChatId = chatId) cache.Offices
          |> Option.toList
          |> channel.Reply

          return! cacheHandler cache
        | CacheCommand.TryGetEmployerByChatId (chatId, channel) ->

          finder (fun e -> e.ChatId = chatId) cache.Employers channel

          return! cacheHandler cache

        | CacheCommand.TryGetManagerByChatId (chatId, channel) ->

          finder (fun (m: Manager) -> m.ChatId = chatId) cache.Managers channel

          return! cacheHandler cache

        | CacheCommand.TryGetMessageByChatId (chatId, channel) ->

          finder
            (fun (m: Funogram.Telegram.Types.Message) -> m.Chat.Id = %chatId)
            cache.Messages
            channel

          return! cacheHandler cache

        | CacheCommand.TryAddDeletionItemInDb (item, channel) ->

          let result =
            result {
              let ticks = let x = item.Time in x.Ticks
              let! _ = Database.insertDeletionItem ctx.Conn item
              and! itemDto = Database.selectDeletionItemByTimeTicks ctx.Conn ticks
              return itemDto
            }

          match result with
          | Ok itemDto ->
            let employer =
              match List.tryFind (fun (m: Manager) -> %m.ChatId = itemDto.ChatId) cache.Managers
                with
              | Some m ->
                let office = List.find (fun o -> %o.OfficeId = itemDto.OfficeId) cache.Offices
                Employer.create m.FirstName m.LastName office m.ChatId
              | None -> List.find (fun e -> %e.ChatId = itemDto.ChatId) cache.Employers

            let itemDomain = DeletionItemDto.toDomainWithEmployer itemDto employer
            itemDomain |> Ok |> channel.Reply

            return!
              cacheHandler { cache with Cache.DeletionItems = itemDomain :: cache.DeletionItems }
          | Error err ->
            err |> Error |> channel.Reply
            return! cacheHandler cache

        | CacheCommand.TryAddOrUpdateMessageInDb (message, channel) ->

          let isExist =
            List.exists
              (fun (m: Funogram.Telegram.Types.Message) -> m.Chat.Id = message.ChatId)
              cache.Messages

          let inline resultHandler result =
            match result with
            | Ok _ ->
              let msg = MessageDto.toDomain message

              let updatedMessageList =
                List.filter
                  (fun (m: Funogram.Telegram.Types.Message) -> m.Chat.Id = message.ChatId)
                  cache.Messages
                |> fun l -> msg :: l

              msg |> Ok |> channel.Reply
              cacheHandler { cache with Cache.Messages = updatedMessageList }
            | Error err ->
              err |> Error |> channel.Reply
              cacheHandler cache

          if isExist then
            let result = Database.updateMessage ctx.Conn message
            return! resultHandler result
          else

            let result = Database.insertMessage ctx.Conn message
            return! resultHandler result

      }

    cacheHandler cache

  let inline private reply (cacheActor: MailboxProcessor<CacheCommand>) asyncReplyChannel =
    cacheActor.PostAndReply(asyncReplyChannel)

  let getOffices
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =
    env.Logger.Debug $"Get offices from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetOffices(channel)

  let getOfficesAsync env cacheActor = task { return getOffices env cacheActor }

  let getDeletionItems
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =
    env.Logger.Debug $"Get deletion items from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetDeletionItems(channel)

  let getDeletionItemsAsync env cacheActor = task { return getDeletionItems env cacheActor }

  let getEmployers
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =
    env.Logger.Debug $"Get employers from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetEmployers(channel)

  let getEmployersAsync env cacheActor = task { return getEmployers env cacheActor }

  let getManagers
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =
    env.Logger.Debug $"Get managers from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetManagers(channel)

  let getMangersAsync env cacheActor = task { return getManagers env cacheActor }

  let getMessages
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =
    env.Logger.Debug $"Get messages from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetMessages(channel)

  let getMessagesAsync env cacheActor = task { return getMessages env cacheActor }

  let getOfficesByManagerId
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (chatId: UMX.ChatId)
    =
    env.Logger.Debug $"Get offices by manager id {chatId} from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.GetOfficesByManagerId(chatId, channel)

  let getOfficesByManagerIdAsync env cacheActor chatId =
    task { return getOfficesByManagerId env cacheActor chatId }

  let tryGetEmployerByChatId
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (chatId: UMX.ChatId)
    =
    env.Logger.Debug $"Get employer by chat id {chatId} from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryGetEmployerByChatId(chatId, channel)

  let tryGetEmployerByChatIdAsync env cacheActor chatId =
    task { return tryGetEmployerByChatId env cacheActor chatId }

  let tryGetManagerByChatId
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (chatId: UMX.ChatId)
    =
    env.Logger.Debug $"Get manager by chat id {chatId} from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryGetManagerByChatId(chatId, channel)

  let tryGetManagerByChatIdAsync env cacheActor chatId =
    task { return tryGetManagerByChatId env cacheActor chatId }

  let tryGetMessageByChatId
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (chatId: UMX.ChatId)
    =
    env.Logger.Debug $"Get message by chat id {chatId} from cache"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryGetMessageByChatId(chatId, channel)

  let tryGetMessageByChatIdAsync env cacheActor chatId =
    task { return tryGetMessageByChatId env cacheActor chatId }

  let tryAddOfficeInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (recordOffice: RecordOffice)
    =
    env.Logger.Debug $"Try add office in database name {recordOffice.OfficeName}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryAddOfficeInDb(recordOffice, channel)

  let tryAddOfficeInDbAsync env cacheActor recordOffice =
    task { return tryAddOfficeInDb env cacheActor recordOffice }

  let tryAddEmployerInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (recordEmployer: RecordEmployer)
    =
    env.Logger.Debug $"Try add employer in database chat id {recordEmployer.ChatId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryAddEmployerInDb(recordEmployer, channel)

  let tryAddEmployerInDbAsync env cacheActor recordEmployer =
    task { return tryAddEmployerInDb env cacheActor recordEmployer }

  let tryAddManagerInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (managerDto: ManagerDto)
    =
    env.Logger.Debug $"Try add manager in database chat id {managerDto.ChatId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryAddManagerInDb(managerDto, channel)

  let tryAddManagerInDbAsync env cacheActor managerDto =
    task { return tryAddManagerInDb env cacheActor managerDto }


  let tryAddDeletionItemInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (recordDeletionItem: RecordDeletionItem)
    =
    env.Logger.Debug
      $"Try add deletion item in database name {recordDeletionItem.ItemName} office id {recordDeletionItem.OfficeId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryAddDeletionItemInDb(recordDeletionItem, channel)

  let tryAddDeletionItemInDbAsync env cacheActor recordDeletionItem =
    task { return tryAddDeletionItemInDb env cacheActor recordDeletionItem }

  let tryUpdateEmployerApprovedInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (employer: Employer)
    isApproved
    =
    env.Logger.Debug $"Chage approved for employer chat id {employer.ChatId} to {isApproved}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryUpdateEmployerApprovedInDb(employer, isApproved, channel)

  let tryUpdateEmployerApprovedInDbAsync env cacheActor employer isApproved =
    task { return tryUpdateEmployerApprovedInDb env cacheActor employer isApproved }

  let trySetDeletionOnItemsOfOffice
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (officeId: OfficeId)
    =
    env.Logger.Debug $"Try deletion all items in office with id {officeId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TrySetDeletionOnItemsOfOffice(officeId, channel)

  let trySetDeletionOnItemsOfOfficeAsync env cacheActor officeId =
    task { return trySetDeletionOnItemsOfOffice env cacheActor officeId }

  let tryHideDeletionItem
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (deletionId: DeletionId)
    =
    env.Logger.Debug $"Try hide item with id {deletionId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryHideDeletionItem(deletionId, channel)

  let tryHideDeletionItemAsync env cacheActor deletionId =
    task { return tryHideDeletionItem env cacheActor deletionId }

  let tryDeleteOffice
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (officeId: OfficeId)
    =
    env.Logger.Debug $"Try delete office with id {officeId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryDeleteOffice(officeId, channel)

  let tryDeleteOfficeAsync env cacheActor officeId =
    task { return tryDeleteOffice env cacheActor officeId }

  let tryAddOrUpdateMessageInDb
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    (message: MessageDto)
    =
    env.Logger.Debug $"Try add or update message in db with id {message.ChatId}"

    reply cacheActor
    ^ fun channel -> CacheCommand.TryAddOrUpdateMessageInDb(message, channel)

  let tryAddOrUpdateMessageInDbAsync env cacheActor message =
    task { return tryAddOrUpdateMessageInDb env cacheActor message }

  let buildCacheInterface
    (env: #WorkTelegram.Infrastucture.AppEnv.ILog)
    (cacheActor: MailboxProcessor<CacheCommand>)
    =

    { new WorkTelegram.Infrastucture.AppEnv.ICache with
        member __.Cache =
          { new WorkTelegram.Infrastucture.AppEnv.IAppCache with
              member __.GetOffices() = getOffices env cacheActor
              member __.GetDeletionItems() = getDeletionItems env cacheActor
              member __.GetEmployers() = getEmployers env cacheActor
              member __.GetManagers() = getManagers env cacheActor
              member __.GetMessages() = getMessages env cacheActor
              member __.GetOfficesByManagerId chatId = getOfficesByManagerId env cacheActor chatId
              member __.TryGetEmployerByChatId chatId = tryGetEmployerByChatId env cacheActor chatId
              member __.TryGetManagerByChatId chatId = tryGetManagerByChatId env cacheActor chatId
              member __.TryGetMessageByChatId chatId = tryGetMessageByChatId env cacheActor chatId
              member __.TryAddOfficeInDb officeRecord = tryAddOfficeInDb env cacheActor officeRecord

              member __.TryAddEmployerInDb employerRecord =
                tryAddEmployerInDb env cacheActor employerRecord

              member __.TryAddManagerInDb managerDto = tryAddManagerInDb env cacheActor managerDto

              member __.TryAddDeletionItemInDb deletionItemRecord =
                tryAddDeletionItemInDb env cacheActor deletionItemRecord

              member __.TryAddOrUpdateMessageInDb messageDto =
                tryAddOrUpdateMessageInDb env cacheActor messageDto

              member __.TryUpdateEmployerApprovedInDb employer isApproved =
                tryUpdateEmployerApprovedInDb env cacheActor employer isApproved

              member __.TrySetDeletionOnItemsOfOffice officeId =
                trySetDeletionOnItemsOfOffice env cacheActor officeId

              member __.TryHideDeletionItem deletionId =
                tryHideDeletionItem env cacheActor deletionId

              member __.TryDeleteOffice officeId = tryDeleteOffice env cacheActor officeId } }
