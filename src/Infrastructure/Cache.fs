namespace WorkTelegram.Infrastructure

open WorkTelegram.Core
open FSharp.UMX
open WorkTelegram.Infrastructure

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type CacheCommand =
  | GetOffices of AsyncReplyChannel<Office list>
  | GetDeletionItems of AsyncReplyChannel<DeletionItem list>
  | GetEmployers of AsyncReplyChannel<Employer list>
  | GetManagers of AsyncReplyChannel<Manager list>
  | GetMessages of AsyncReplyChannel<Funogram.Telegram.Types.Message list>
  | GetOfficesByManagerId of ChatId * AsyncReplyChannel<Office list>
  | TryGetEmployerByChatId of ChatId * AsyncReplyChannel<Result<Employer, BusinessError>>
  | TryGetManagerByChatId of ChatId * AsyncReplyChannel<Result<Manager, BusinessError>>
  | TryGetMessageByChatId of
    ChatId *
    AsyncReplyChannel<Result<Funogram.Telegram.Types.Message, BusinessError>>
  | TryAddOfficeInDb of RecordOffice * AsyncReplyChannel<Result<Office, AppError>>
  | TryAddEmployerInDb of RecordEmployer * AsyncReplyChannel<Result<Employer, AppError>>
  | TryAddManagerInDb of ManagerDto * AsyncReplyChannel<Result<Manager, AppError>>
  | TryAddDeletionItemInDb of RecordDeletionItem * AsyncReplyChannel<Result<DeletionItem, AppError>>
  | TryAddOrUpdateMessageInDb of
    MessageDto *
    AsyncReplyChannel<Result<Funogram.Telegram.Types.Message, AppError>>
  | TryUpdateEmployerApprovedInDb of
    Employer *
    bool *
    AsyncReplyChannel<bool>
  | TrySetDeletionOnItemsOfOffice of OfficeId * AsyncReplyChannel<bool>
  | TryHideDeletionItem of DeletionId * AsyncReplyChannel<bool>
  | TryDeleteOffice of OfficeId * AsyncReplyChannel<bool>

[<NoComparison>]
type CacheContext = { Database: IDb; Logger: ILog }

[<NoComparison>]
type private Cache =
  { Employers: Employer list
    Offices: Office list
    Managers: Manager list
    DeletionItems: DeletionItem list
    Messages: Funogram.Telegram.Types.Message list }

module Cache =

  let private initializationCache (ctx: CacheContext) =

    let employersDto = Database.selectEmployers ctx.Database
    let managersDto = Database.selectManagers ctx.Database
    let officesDto = Database.selectOffices ctx.Database
    let deletionItemsDto = Database.selectDeletionItems ctx.Database
    let messagesDto = Database.selectMessages ctx.Database

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
              let! _ = Database.insertDeletionItem ctx.Database item
              and! itemDto = Database.selectDeletionItemByTimeTicks ctx.Database ticks
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

        | CacheCommand.TryAddManagerInDb (managerDto, channel) ->

          let result = Database.insertManager ctx.Database managerDto

          match result with
          | Ok _ ->
            let manager = ManagerDto.toDomain managerDto
            manager |> Ok |> channel.Reply
            return! cacheHandler { cache with Cache.Managers = manager :: cache.Managers }
          | Error err -> err |> Error |> channel.Reply

        | CacheCommand.TryAddOfficeInDb (officeRecord, channel) ->

          let result =
            result {
              let! _ = Database.insertOffice ctx.Database officeRecord
              let chatIdDto = ChatIdDto.fromDomain %officeRecord.ManagerChatId
              let! manager = Database.selectManagerByChatId ctx.Database chatIdDto
              and! officeDto = Database.selectOfficeByName ctx.Database officeRecord.OfficeName
              return (ManagerDto.toDomain manager, officeDto)
            }

          match result with
          | Ok (m, o) ->
            let office = OfficeDto.toDomainWithManager o m
            office |> Ok |> channel.Reply
            return! cacheHandler { cache with Cache.Offices = office :: cache.Offices }
          | Error err ->
            err |> Error |> channel.Reply
            return! cacheHandler cache

        | CacheCommand.TryAddEmployerInDb (employerRecord, channel) ->
          let result = Database.insertEmployer ctx.Database employerRecord
          match result with
          | Ok _ ->
            let office = cache.Offices |> List.find (fun o -> %o.OfficeId = employerRecord.OfficeId)
            let employer =
              { ChatId = employerRecord.ChatId
                FirstName = employerRecord.FirstName
                LastName = employerRecord.LastName
                IsApproved = false
                OfficeId = employerRecord.OfficeId }
              |> EmployerDto.toDomainWithOffice office
            employer |> Ok |> channel.Reply
            return! cacheHandler { cache with Cache.Employers = employer :: cache.Employers }
          | Error err ->
            err |> Error |> channel.Reply
            return! cacheHandler cache

        | CacheCommand.TryHideDeletionItem (deletionId, channel) ->
          let result = Database.setTrueForHiddenFieldOfItem ctx.Database %deletionId

          match result with
          | Ok _ ->
            channel.Reply true

            let updatedList =
              cache.DeletionItems
              |> List.map (fun di ->
                if di.DeletionId = deletionId then
                  { di with IsHidden = true }
                else
                  di)

            return! cacheHandler { cache with Cache.DeletionItems = updatedList }
          | Error _ ->
            channel.Reply false
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
            let result = Database.updateMessage ctx.Database message
            return! resultHandler result
          else

            let result = Database.insertMessage ctx.Database message
            return! resultHandler result
            
        | CacheCommand.TryDeleteOffice (officeId, channel) ->
          let result = Database.deleteOffice ctx.Database %officeId
          match result with
          | Ok _ ->
            let updatedList =
              cache.Offices
              |> List.filter (fun o -> o.OfficeId <> officeId)
            channel.Reply true
            return! cacheHandler { cache with Cache.Offices = updatedList }
          | Error _ ->
            channel.Reply false
            return! cacheHandler cache
            
        | CacheCommand.TrySetDeletionOnItemsOfOffice(officeId, channel) ->
          let result = Database.setTrueForDeletionFieldOfOfficeItems ctx.Database %officeId
          match result with
          | Ok _ ->
            channel.Reply true
            let updatedList =
              cache.DeletionItems
              |> List.map (fun di ->
                if di.Employer.Office.OfficeId = officeId then
                  { di with IsDeletion = true }
                else
                  di)
            return! cacheHandler { cache with Cache.DeletionItems = updatedList }
          | Error _ ->
            channel.Reply false
            return! cacheHandler cache
            
        | CacheCommand.TryUpdateEmployerApprovedInDb(employer, isApproved, channel) ->
          let chatIdDto = ChatIdDto.fromDomain employer.ChatId
          let result = Database.updateEmployerApprovedByChatId ctx.Database chatIdDto isApproved
          match result with
          | Ok _ ->
            channel.Reply true
            return! cacheHandler cache
          | Error _ ->
            channel.Reply false
            return! cacheHandler cache
      }

    cacheHandler cache

  let inline private reply (env: #ICache<_>) asyncReplyChannel =
    env.Cache.Mailbox.PostAndReply(asyncReplyChannel)

  let getOffices env =
    Logger.debug env $"Get offices from cache"

    reply env CacheCommand.GetOffices

  let getOfficesAsync env = task { return getOffices env }

  let getDeletionItems env =
    Logger.debug env $"Get deletion items from cache"

    reply env CacheCommand.GetDeletionItems

  let getDeletionItemsAsync env = task { return getDeletionItems env }

  let getEmployers env =
    Logger.debug env $"Get employers from cache"

    reply env CacheCommand.GetEmployers

  let getEmployersAsync env = task { return getEmployers env }

  let getManagers env =
    Logger.debug env $"Get managers from cache"

    reply env CacheCommand.GetManagers

  let getMangersAsync env = task { return getManagers env }

  let getMessages env =
    Logger.debug env $"Get messages from cache"

    reply env CacheCommand.GetMessages

  let getMessagesAsync env = task { return getMessages env }

  let getOfficesByManagerId env (chatId: ChatId) =
    Logger.debug env $"Get offices by manager id {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.GetOfficesByManagerId(chatId, channel)

  let getOfficesByManagerIdAsync env chatId = task { return getOfficesByManagerId env chatId }

  let tryGetEmployerByChatId env (chatId: ChatId) =
    Logger.debug env $"Get employer by chat id {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.TryGetEmployerByChatId(chatId, channel)

  let tryGetEmployerByChatIdAsync env chatId = task { return tryGetEmployerByChatId env chatId }

  let tryGetManagerByChatId env (chatId: ChatId) =
    Logger.debug env $"Get manager by chat id {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.TryGetManagerByChatId(chatId, channel)

  let tryGetManagerByChatIdAsync env chatId = task { return tryGetManagerByChatId env chatId }

  let tryGetMessageByChatId env (chatId: ChatId) =
    Logger.debug env $"Get message by chat id {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.TryGetMessageByChatId(chatId, channel)

  let tryGetMessageByChatIdAsync env chatId = task { return tryGetMessageByChatId env chatId }

  let tryAddOfficeInDb env (recordOffice: RecordOffice) =
    Logger.debug env $"Try add office in database name {recordOffice.OfficeName}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOfficeInDb(recordOffice, channel)

  let tryAddOfficeInDbAsync env recordOffice = task { return tryAddOfficeInDb env recordOffice }

  let tryAddEmployerInDb env (recordEmployer: RecordEmployer) =
    Logger.debug env $"Try add employer in database chat id {recordEmployer.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddEmployerInDb(recordEmployer, channel)

  let tryAddEmployerInDbAsync env recordEmployer =
    task { return tryAddEmployerInDb env recordEmployer }

  let tryAddManagerInDb env (managerDto: ManagerDto) =
    Logger.debug env $"Try add manager in database chat id {managerDto.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddManagerInDb(managerDto, channel)

  let tryAddManagerInDbAsync env managerDto = task { return tryAddManagerInDb env managerDto }


  let tryAddDeletionItemInDb env (recordDeletionItem: RecordDeletionItem) =
    Logger.debug
      env
      $"Try add deletion item in database name {recordDeletionItem.ItemName} office id {recordDeletionItem.OfficeId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddDeletionItemInDb(recordDeletionItem, channel)

  let tryAddDeletionItemInDbAsync env recordDeletionItem =
    task { return tryAddDeletionItemInDb env recordDeletionItem }

  let tryUpdateEmployerApprovedInDb env (employer: Employer) isApproved =
    Logger.debug env $"Change approved for employer chat id {employer.ChatId} to {isApproved}"

    reply env
    ^ fun channel -> CacheCommand.TryUpdateEmployerApprovedInDb(employer, isApproved, channel)

  let tryUpdateEmployerApprovedInDbAsync env employer isApproved =
    task { return tryUpdateEmployerApprovedInDb env employer isApproved }

  let trySetDeletionOnItemsOfOffice env (officeId: OfficeId) =
    Logger.debug env $"Try deletion all items in office with id {officeId}"

    reply env
    ^ fun channel -> CacheCommand.TrySetDeletionOnItemsOfOffice(officeId, channel)

  let trySetDeletionOnItemsOfOfficeAsync env officeId =
    task { return trySetDeletionOnItemsOfOffice env officeId }

  let tryHideDeletionItem env (deletionId: DeletionId) =
    Logger.debug env $"Try hide item with id {deletionId}"

    reply env
    ^ fun channel -> CacheCommand.TryHideDeletionItem(deletionId, channel)

  let tryHideDeletionItemAsync env deletionId =
    task { return tryHideDeletionItem env deletionId }

  let tryDeleteOffice env (officeId: OfficeId) =
    Logger.debug env $"Try delete office with id {officeId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeleteOffice(officeId, channel)

  let tryDeleteOfficeAsync env officeId = task { return tryDeleteOffice env officeId }

  let tryAddOrUpdateMessageInDb env (message: MessageDto) =
    Logger.debug env $"Try add or update message in db with id {message.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOrUpdateMessageInDb(message, channel)

  let tryAddOrUpdateMessageInDbAsync env message =
    task { return tryAddOrUpdateMessageInDb env message }

  let ICacheBuilder (mailbox: MailboxProcessor<'a>) =

    { new ICache<'a> with
        member _.Cache =
          { new IAppCache<'a> with
              member _.Mailbox = mailbox } }
