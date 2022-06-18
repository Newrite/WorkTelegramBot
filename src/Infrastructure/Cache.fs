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
  | GetTelegramMessages of AsyncReplyChannel<TelegramMessage list>
  | GetOfficesByManagerId of ChatId * AsyncReplyChannel<Office list>
  | TryGetEmployerByChatId of ChatId * AsyncReplyChannel<Employer option>
  | TryGetManagerByChatId of ChatId * AsyncReplyChannel<Manager option>
  | TryGetTelegramMessageByChatId of ChatId * AsyncReplyChannel<TelegramMessage option>
  | TryAddOffice of Office * AsyncReplyChannel<Office option>
  | TryAddEmployer of Employer * AsyncReplyChannel<Employer option>
  | TryAddManager of Manager * AsyncReplyChannel<Manager option>
  | TryAddDeletionItem of DeletionItem * AsyncReplyChannel<DeletionItem option>
  | TryAddOrUpdateTelegramMessage of TelegramMessage * AsyncReplyChannel<bool>
  | TryChangeEmployerApproved of Employer * bool * AsyncReplyChannel<bool>
  | TryDeletionDeletionItemsOfOffice of OfficeId * AsyncReplyChannel<ExtBool>
  | TryDeleteDeletionItem of DeletionId * AsyncReplyChannel<bool>
  | TryDeleteOffice of OfficeId * AsyncReplyChannel<bool>
  | TryDeleteTelegramMessage of ChatId * AsyncReplyChannel<bool>
  | IsApprovedEmployer of Employer * AsyncReplyChannel<bool>

[<NoComparison>]
type CacheContext = { Database: IDb; Logger: ILog }

[<NoComparison>]
type private Cache =
  { Employers: Map<ChatId, Employer>
    Offices: Map<OfficeId, Office>
    Managers: Map<ChatId, Manager>
    DeletionItems: Map<DeletionId, DeletionItem>
    Messages: Map<ChatId, TelegramMessage> }

exception private CacheUnmatchedException of string

module Cache =

  open ErrorPatterns

  let inline private line () = __SOURCE_FILE__ + ":" + __LINE__

  let private errorHandler env (error: AppError) source =
    match error with
    | ErrNotFoundInDatabase searchedType ->
      Logger.warning env $"{source} Searched value not found in database, type {searchedType.Name}"
    | ErrIncorrectMacAddress incorrectMac ->
      Logger.warning env $"{source} Mac address {incorrectMac} is incorrect"
    | ErrIncorrectParsePositiveNumber incorrectString ->
      Logger.warning env $"Incorrect string ({incorrectString}) for parse to positive number"
    | ErrNumberMustBePositive incorrectNumber ->
      Logger.warning env $"{source} Incorrect number ({incorrectNumber}), number must be positive"
    | ErrDbConnectionError dbConnError ->
      Logger.error
        env
        $"{source} Database connection error,
            connection string {dbConnError.ConnectionString},
            message {dbConnError.Error.Message}"
    | ErrDbExecutionError dbExeError ->
      Logger.error
        env
        $"{source} Database execution error,
            statement {dbExeError.Statement},
            message {dbExeError.Error.Message}"
    | ErrDbTransactionError dbTranError ->
      Logger.error
        env
        $"{source} Database transaction error,
            step {dbTranError.Step},
            message {dbTranError.Error.Message}"
    | ErrDataReaderCastError rdCastError ->
      Logger.error
        env
        $"{source} Data reader cast error,
            field name {rdCastError.FieldName},
            message {rdCastError.Error.Message}"
    | ErrDataReaderOutOfRangeError rdRangeError ->
      Logger.error
        env
        $"{source} Data reader range error,
            field name {rdRangeError.FieldName},
            message {rdRangeError.Error.Message}"
    | _ ->

    Logger.fatal env $"{source} Error: {error} not matched in cache error handler"

    CacheUnmatchedException($"{source} Error: {error} not matched in cache error handler")
    |> raise

  let private initializationCache (ctx: CacheContext) =

    let inline handleSelect result =
      match result with
      | Ok list -> list
      | Error err ->

      errorHandler ctx.Logger err (line ())
      []

    let employersDto =
      Database.selectEmployers ctx.Database
      |> handleSelect

    let managersDto =
      Database.selectManagers ctx.Database
      |> handleSelect

    let officesDto =
      Database.selectOffices ctx.Database
      |> handleSelect

    let deletionItemsDto =
      Database.selectDeletionItems ctx.Database
      |> handleSelect

    let messagesDto =
      Database.selectTelegramMessages ctx.Database
      |> handleSelect

    let managers =
      [ for manager in managersDto do
          let key: ChatId = %manager.ChatId
          key, ManagerDto.toDomain manager ]
      |> Map.ofList

    let offices =
      [ for office in officesDto do
          let key: OfficeId = %office.OfficeId
          key, OfficeDto.toDomainWithManager office managers[%office.ManagerId] ]
      |> Map.ofList

    let employers =
      [ for employer in employersDto do
          let key: ChatId = %employer.ChatId
          key, EmployerDto.toDomainWithOffice offices[%employer.OfficeId] employer ]
      |> Map.ofList

    let deletionItems =
      [ for deletionItem in deletionItemsDto do
          let key: DeletionId = %deletionItem.DeletionId

          let employer =
            let chatId: ChatId = %deletionItem.ChatId

            if employers.ContainsKey(chatId) then
              employers[chatId]
            else

            let officeId: OfficeId = %deletionItem.OfficeId
            Manager.asEmployer managers[chatId] offices[officeId]

          key, DeletionItemDto.toDomainWithEmployer deletionItem employer ]
      |> Map.ofList

    let messages =
      [ for message in messagesDto do
          let key: ChatId = %message.ChatId
          key, TelegramMessageDto.toDomain message ]
      |> Map.ofList

    let cache =
      { Employers = employers
        Managers = managers
        Offices = offices
        DeletionItems = deletionItems
        Messages = messages }

    cache

  let cacheActor (ctx: CacheContext) (inbox: MailboxProcessor<CacheCommand>) =

    let cache = initializationCache ctx

    let inline finder predicate collection (channel: AsyncReplyChannel<'a option>) =

      let entity = List.tryFind predicate collection

      channel.Reply entity

    let rec cacheHandler cache =
      async {

        try
          match! inbox.Receive() with
          | CacheCommand.GetOffices channel ->
            cache.Offices
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.GetDeletionItems channel ->
            cache.DeletionItems
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.GetEmployers channel ->
            cache.Employers
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.GetManagers channel ->
            cache.Managers
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.GetTelegramMessages channel ->
            cache.Messages
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.GetOfficesByManagerId (chatId, channel) ->
            cache.Offices
            |> Map.filter (fun _ office -> office.Manager.ChatId = chatId)
            |> Map.toList
            |> List.map snd
            |> channel.Reply

            return! cacheHandler cache
          | CacheCommand.TryGetEmployerByChatId (chatId, channel) ->

            (if cache.Employers.ContainsKey(chatId) then
               Some cache.Employers[chatId]
             else
               None)
            |> channel.Reply

            return! cacheHandler cache

          | CacheCommand.TryGetManagerByChatId (chatId, channel) ->

            (if cache.Managers.ContainsKey(chatId) then
               Some cache.Managers[chatId]
             else
               None)
            |> channel.Reply

            return! cacheHandler cache

          | CacheCommand.TryGetTelegramMessageByChatId (chatId, channel) ->

            (if cache.Messages.ContainsKey(chatId) then
               Some cache.Messages[chatId]
             else
               None)
            |> channel.Reply

            return! cacheHandler cache

          | CacheCommand.TryAddDeletionItem (item, channel) ->

            let itemDto = DeletionItemDto.fromDomain item

            let result = Database.insertDeletionItem ctx.Database itemDto

            match result with
            | Ok _ ->
              item |> Some |> channel.Reply

              return!
                cacheHandler
                  { cache with
                      Cache.DeletionItems = Map.add item.DeletionId item cache.DeletionItems }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddManager (manager, channel) ->

            let managerDto = ManagerDto.fromDomain manager

            let result = Database.insertManager ctx.Database managerDto

            match result with
            | Ok _ ->
              manager |> Some |> channel.Reply

              return!
                cacheHandler
                  { cache with Cache.Managers = Map.add manager.ChatId manager cache.Managers }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddOffice (office, channel) ->

            let officeDto = OfficeDto.fromDomain office

            let result = Database.insertOffice ctx.Database officeDto

            match result with
            | Ok _ ->
              office |> Some |> channel.Reply

              return!
                cacheHandler
                  { cache with Cache.Offices = Map.add office.OfficeId office cache.Offices }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddEmployer (employer, channel) ->
            let employerDto = EmployerDto.fromDomain employer

            let result = Database.insertEmployer ctx.Database employerDto

            match result with
            | Ok _ ->

              employer |> Some |> channel.Reply

              return!
                cacheHandler
                  { cache with Cache.Employers = Map.add employer.ChatId employer cache.Employers }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryDeleteDeletionItem (deletionId, channel) ->
            let result = Database.hideDeletionItem ctx.Database %deletionId

            match result with
            | Ok _ ->
              channel.Reply true

              let updatedItem = { cache.DeletionItems[deletionId] with IsHidden = true }

              return!
                cacheHandler
                  { cache with
                      Cache.DeletionItems =
                        Map.add updatedItem.DeletionId updatedItem cache.DeletionItems }
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddOrUpdateTelegramMessage (message, channel) ->

            let messageDto = TelegramMessageDto.fromDomain message
            let chatId: ChatId = %messageDto.ChatId

            let inline resultHandler result =
              match result with
              | Ok _ ->

                channel.Reply true
                cacheHandler { cache with Cache.Messages = Map.add chatId message cache.Messages }
              | Error err ->

              channel.Reply false
              errorHandler ctx.Logger err (line ())
              cacheHandler cache

            if cache.Messages.ContainsKey(chatId) then
              let result = Database.updateTelegramMessage ctx.Database messageDto
              return! resultHandler result
            else
              let result = Database.insertTelegramMessage ctx.Database messageDto
              return! resultHandler result

          | CacheCommand.TryDeleteOffice (officeId, channel) ->
            let result = Database.deleteOffice ctx.Database %officeId

            match result with
            | Ok _ ->

              channel.Reply true
              return! cacheHandler { cache with Cache.Offices = Map.remove officeId cache.Offices }
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryDeletionDeletionItemsOfOffice (officeId, channel) ->

            if cache.DeletionItems
               |> Map.exists (fun _ v -> not v.IsHidden && not v.IsDeletion && v.Inspired())
               |> not then
              channel.Reply Partial
              return! cacheHandler cache
            else

              let result = Database.deletionDeletionitemsOfOffice ctx.Database %officeId

              match result with
              | Ok _ ->
                channel.Reply True

                let updatedItems =
                  cache.DeletionItems
                  |> Map.map (fun _ v ->
                    if not v.IsDeletion && v.Inspired() then
                      { v with IsDeletion = true }
                    else
                      v)

                return! cacheHandler { cache with Cache.DeletionItems = updatedItems }
              | Error err ->
                channel.Reply False
                errorHandler ctx.Logger err (line ())
                return! cacheHandler cache

          | CacheCommand.TryChangeEmployerApproved (employer, isApproved, channel) ->
            let chatIdDto = ChatIdDto.fromDomain employer.ChatId

            let result =
              Database.updateEmployerApprovedByChatId ctx.Database chatIdDto isApproved

            match result with
            | Ok _ ->
              let updatedEmployer =
                { cache.Employers[employer.ChatId] with IsApproved = isApproved }

              channel.Reply true

              return!
                cacheHandler
                  { cache with Employers = Map.add employer.ChatId updatedEmployer cache.Employers }
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryDeleteTelegramMessage (chatId, channel) ->
            let chatIdDto = ChatIdDto.fromDomain chatId
            let result = Database.deleteTelegramMessage ctx.Database chatIdDto

            match result with
            | Ok _ ->
              channel.Reply true

              return! cacheHandler { cache with Cache.Messages = Map.remove chatId cache.Messages }
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.IsApprovedEmployer (employer, channel) ->

            let employer = cache.Employers |> Map.tryFind employer.ChatId

            match employer with
            | Some e ->
              channel.Reply e.IsApproved
              return! cacheHandler cache
            | None ->
              channel.Reply false
              return! cacheHandler cache
        with
        | exn ->
          Logger.fatal ctx.Logger $"Exception in cache work cycle, message {exn.Message}"
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

  let getTelegramMessages env =
    Logger.debug env $"Get messages from cache"

    reply env CacheCommand.GetTelegramMessages

  let getTelegramMessagesAsync env = task { return getTelegramMessages env }

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

  let tryGetTelegramMessageByChatId env (chatId: ChatId) =
    Logger.debug env $"Get message by chat id {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.TryGetTelegramMessageByChatId(chatId, channel)

  let tryGetTelegramMessageByChatIdAsync env chatId =
    task { return tryGetTelegramMessageByChatId env chatId }

  let tryAddOffice env (office: Office) =
    Logger.debug env $"Try add office in database name {office.OfficeName}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOffice(office, channel)

  let tryAddOfficeAsync env recordOffice = task { return tryAddOffice env recordOffice }

  let tryAddEmployer env (employer: Employer) =
    Logger.debug env $"Try add employer in database chat id {employer.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddEmployer(employer, channel)

  let tryAddEmployerAsync env recordEmployer = task { return tryAddEmployer env recordEmployer }

  let tryAddManager env (manager: Manager) =
    Logger.debug env $"Try add manager in database chat id {manager.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddManager(manager, channel)

  let tryAddManagerAsync env managerDto = task { return tryAddManager env managerDto }


  let tryAddDeletionItem env (deletionItem: DeletionItem) =
    Logger.debug
      env
      $"Try add deletion item in database name {deletionItem.Item.Name}
        office id {deletionItem.Employer.Office.OfficeId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddDeletionItem(deletionItem, channel)

  let tryAddDeletionItemAsync env recordDeletionItem =
    task { return tryAddDeletionItem env recordDeletionItem }

  let tryChangeEmployerApproved env (employer: Employer) isApproved =
    Logger.debug env $"Change approved for employer chat id {employer.ChatId} to {isApproved}"

    reply env
    ^ fun channel -> CacheCommand.TryChangeEmployerApproved(employer, isApproved, channel)

  let tryChangeEmployerApprovedAsync env employer isApproved =
    task { return tryChangeEmployerApproved env employer isApproved }

  let tryDeletionDeletionItemsOfOffice env (officeId: OfficeId) =
    Logger.debug env $"Try deletion all items in office with id {officeId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeletionDeletionItemsOfOffice(officeId, channel)

  let tryDeletionDeletionItemsOfOfficeAsync env officeId =
    task { return tryDeletionDeletionItemsOfOffice env officeId }

  let tryDeleteDeletionItem env (deletionId: DeletionId) =
    Logger.debug env $"Try hide item with id {deletionId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeleteDeletionItem(deletionId, channel)

  let tryDeleteDeletionItemAsync env deletionId =
    task { return tryDeleteDeletionItem env deletionId }

  let tryDeleteOffice env (officeId: OfficeId) =
    Logger.debug env $"Try delete office with id {officeId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeleteOffice(officeId, channel)

  let tryDeleteOfficeAsync env officeId = task { return tryDeleteOffice env officeId }

  let tryAddOrUpdateTelegramMessage env (message: TelegramMessage) =
    Logger.debug env $"Try add or update message in db with id {message.Chat.Id}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOrUpdateTelegramMessage(message, channel)

  let tryAddOrUpdateTelegramMessageAsync env message =
    task { return tryAddOrUpdateTelegramMessage env message }

  let tryDeleteTelegramMessage env (chatId: ChatId) =
    Logger.debug env $"Try delete messageJson with chat id {chatId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeleteTelegramMessage(chatId, channel)

  let tryDeleteTelegramMessageAsync env chatId =
    task { return tryDeleteTelegramMessage env chatId }

  let isApprovedEmployer env employer =
    Logger.debug env $"Check employer approved with chat id {employer.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.IsApprovedEmployer(employer, channel)

  let isApprovedEmployerAsync env employer = task { return isApprovedEmployer env employer }

  let ICacheBuilder (mailbox: MailboxProcessor<'a>) =

    { new ICache<'a> with
        member _.Cache =
          { new IAppCache<'a> with
              member _.Mailbox = mailbox } }
