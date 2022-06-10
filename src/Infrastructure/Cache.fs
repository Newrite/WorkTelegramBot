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
  | TryGetEmployerByChatId of ChatId * AsyncReplyChannel<Employer option>
  | TryGetManagerByChatId of ChatId * AsyncReplyChannel<Manager option>
  | TryGetMessageByChatId of ChatId * AsyncReplyChannel<Funogram.Telegram.Types.Message option>
  | TryAddOfficeInDb of Office * AsyncReplyChannel<Office option>
  | TryAddEmployerInDb of Employer * AsyncReplyChannel<Employer option>
  | TryAddManagerInDb of Manager * AsyncReplyChannel<Manager option>
  | TryAddDeletionItemInDb of DeletionItem * AsyncReplyChannel<DeletionItem option>
  | TryAddOrUpdateMessageInDb of Funogram.Telegram.Types.Message * AsyncReplyChannel<bool>
  | TryUpdateEmployerApprovedInDb of Employer * bool * AsyncReplyChannel<bool>
  | TrySetDeletionOnItemsOfOffice of OfficeId * AsyncReplyChannel<ExtBool>
  | TryHideDeletionItem of DeletionId * AsyncReplyChannel<bool>
  | TryDeleteOffice of OfficeId * AsyncReplyChannel<bool>
  | TryDeleteMessageJson of ChatId * AsyncReplyChannel<bool>
  | IsApprovedEmployer of Employer * AsyncReplyChannel<bool>

[<NoComparison>]
type CacheContext = { Database: IDb; Logger: ILog }

[<NoComparison>]
type private Cache =
  { Employers: Employer list
    Offices: Office list
    Managers: Manager list
    DeletionItems: DeletionItem list
    Messages: Funogram.Telegram.Types.Message list }

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
      Database.selectMessages ctx.Database
      |> handleSelect

    let findManager (id: int64) =
      List.find (fun (m: ManagerDto) -> m.ChatId = id) managersDto

    let findOffice (id: System.Guid) =
      List.find (fun (o: OfficeDto) -> o.OfficeId = id) officesDto

    let findEmployer (id: int64) =
      List.find (fun (e: EmployerDto) -> e.ChatId = id) employersDto

    let managers = List.map ManagerDto.toDomain managersDto

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

    let inline finder predicate collection (channel: AsyncReplyChannel<'a option>) =

      let entity = List.tryFind predicate collection

      channel.Reply entity

    let rec cacheHandler cache =
      async {

        try
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
            List.filter (fun o -> o.Manager.ChatId = chatId) cache.Offices
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

            let itemDto = DeletionItemDto.fromDomain item

            let result = Database.insertDeletionItem ctx.Database itemDto

            match result with
            | Ok _ ->
              item |> Some |> channel.Reply

              return! cacheHandler { cache with Cache.DeletionItems = item :: cache.DeletionItems }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddManagerInDb (manager, channel) ->

            let managerDto = ManagerDto.fromDomain manager

            let result = Database.insertManager ctx.Database managerDto

            match result with
            | Ok _ ->
              manager |> Some |> channel.Reply
              return! cacheHandler { cache with Cache.Managers = manager :: cache.Managers }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddOfficeInDb (office, channel) ->

            let officeDto = OfficeDto.fromDomain office

            let result = Database.insertOffice ctx.Database officeDto

            match result with
            | Ok _ ->
              office |> Some |> channel.Reply
              return! cacheHandler { cache with Cache.Offices = office :: cache.Offices }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddEmployerInDb (employer, channel) ->
            let employerDto = EmployerDto.fromDomain employer false

            let result = Database.insertEmployer ctx.Database employerDto

            match result with
            | Ok _ ->

              employer |> Some |> channel.Reply
              return! cacheHandler { cache with Cache.Employers = employer :: cache.Employers }
            | Error err ->
              channel.Reply None
              errorHandler ctx.Logger err (line ())
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
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryAddOrUpdateMessageInDb (message, channel) ->

            let messageDto = MessageDto.fromDomain message

            let isExist =
              List.exists
                (fun (m: Funogram.Telegram.Types.Message) -> m.Chat.Id = messageDto.ChatId)
                cache.Messages

            let inline resultHandler result =
              match result with
              | Ok _ ->

                let updatedMessageList =
                  List.filter
                    (fun (m: Funogram.Telegram.Types.Message) -> m.Chat.Id <> messageDto.ChatId)
                    cache.Messages
                  |> fun l -> message :: l

                channel.Reply true
                cacheHandler { cache with Cache.Messages = updatedMessageList }
              | Error err ->
                channel.Reply false
                errorHandler ctx.Logger err (line ())
                cacheHandler cache

            if isExist then
              let result = Database.updateMessage ctx.Database messageDto
              return! resultHandler result
            else

              let result = Database.insertMessage ctx.Database messageDto
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
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TrySetDeletionOnItemsOfOffice (officeId, channel) ->

            if cache.DeletionItems
               |> List.tryFind (fun di -> di.IsDeletion |> not && di.IsHidden |> not)
               |> Option.isSome
               |> not then
              channel.Reply Partial
              return! cacheHandler cache
            else

              let result = Database.setTrueForDeletionFieldOfOfficeItems ctx.Database %officeId

              match result with
              | Ok _ ->
                channel.Reply True

                let updatedList =
                  cache.DeletionItems
                  |> List.map (fun di ->
                    if di.Employer.Office.OfficeId = officeId then
                      { di with IsDeletion = true }
                    else
                      di)

                return! cacheHandler { cache with Cache.DeletionItems = updatedList }
              | Error err ->
                channel.Reply False
                errorHandler ctx.Logger err (line ())
                return! cacheHandler cache

          | CacheCommand.TryUpdateEmployerApprovedInDb (employer, isApproved, channel) ->
            let chatIdDto = ChatIdDto.fromDomain employer.ChatId

            let result =
              Database.updateEmployerApprovedByChatId ctx.Database chatIdDto isApproved

            match result with
            | Ok _ ->
              channel.Reply true
              return! cacheHandler cache
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.TryDeleteMessageJson (chatId, channel) ->
            let chatIdDto = ChatIdDto.fromDomain chatId
            let result = Database.deleteMessageJson ctx.Database chatIdDto

            match result with
            | Ok _ ->
              channel.Reply true

              let updatedList =
                cache.Messages
                |> List.filter (fun msg -> msg.Chat.Id <> %chatId)

              return! cacheHandler { cache with Cache.Messages = updatedList }
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
              return! cacheHandler cache

          | CacheCommand.IsApprovedEmployer (employer, channel) ->
            let chatIdDto = ChatIdDto.fromDomain employer.ChatId
            let result = Database.selectEmployerByChatId ctx.Database chatIdDto

            match result with
            | Ok e ->
              channel.Reply e.IsApproved
              return! cacheHandler cache
            | Error err ->
              channel.Reply false
              errorHandler ctx.Logger err (line ())
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

  let tryAddOfficeInDb env (office: Office) =
    Logger.debug env $"Try add office in database name {office.OfficeName}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOfficeInDb(office, channel)

  let tryAddOfficeInDbAsync env recordOffice = task { return tryAddOfficeInDb env recordOffice }

  let tryAddEmployerInDb env (employer: Employer) =
    Logger.debug env $"Try add employer in database chat id {employer.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddEmployerInDb(employer, channel)

  let tryAddEmployerInDbAsync env recordEmployer =
    task { return tryAddEmployerInDb env recordEmployer }

  let tryAddManagerInDb env (manager: Manager) =
    Logger.debug env $"Try add manager in database chat id {manager.ChatId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddManagerInDb(manager, channel)

  let tryAddManagerInDbAsync env managerDto = task { return tryAddManagerInDb env managerDto }


  let tryAddDeletionItemInDb env (deletionItem: DeletionItem) =
    Logger.debug
      env
      $"Try add deletion item in database name {deletionItem.Item.Name}
        office id {deletionItem.Employer.Office.OfficeId}"

    reply env
    ^ fun channel -> CacheCommand.TryAddDeletionItemInDb(deletionItem, channel)

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

  let tryAddOrUpdateMessageInDb env (message: Funogram.Telegram.Types.Message) =
    Logger.debug env $"Try add or update message in db with id {message.Chat.Id}"

    reply env
    ^ fun channel -> CacheCommand.TryAddOrUpdateMessageInDb(message, channel)

  let tryAddOrUpdateMessageInDbAsync env message =
    task { return tryAddOrUpdateMessageInDb env message }

  let tryDeleteMessageJson env (chatId: ChatId) =
    Logger.debug env $"Try delete messageJson with chat id {chatId}"

    reply env
    ^ fun channel -> CacheCommand.TryDeleteMessageJson(chatId, channel)

  let tryDeleteMessageJsonAsync env chatId = task { return tryDeleteMessageJson env chatId }

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
