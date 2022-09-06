namespace WorkTelegram.Infrastructure

open System.Threading.Tasks

open WorkTelegram.Core
open FSharp.UMX
open WorkTelegram.Infrastructure
open WorkTelegram.Core.Field

[<Interface>]
type IRepository<'CacheCommand> =
  abstract Cache: Agent<'CacheCommand>

[<Interface>]
type IRep<'CacheCommand> =
  abstract Repository: IRepository<'CacheCommand>

module Repository =

  exception private RepositoryUnmatchedException of string

  let private cache (env: #IRep<_>) = env.Repository.Cache

  open ErrorPatterns

  let errorHandler env (error: AppError) =
    match error with
    | ErrNotFoundInDatabase searchedType ->
      Logger.warning env $"Searched value not found in database, type {searchedType.Name}"
    | ErrIncorrectMacAddress incorrectMac ->
      Logger.warning env $"Mac address {incorrectMac} is incorrect"
    | ErrIncorrectParsePositiveNumber incorrectString ->
      Logger.warning env $"Incorrect string ({incorrectString}) for parse to positive number"
    | ErrNumberMustBePositive incorrectNumber ->
      Logger.warning env $"Incorrect number ({incorrectNumber}), number must be positive"
    | ErrDbConnectionError dbConnError ->
      Logger.error
        env
        $"Database connection error,
            connection string {dbConnError.ConnectionString},
            message {dbConnError.Error.Message}"
    | ErrDbExecutionError dbExeError ->
      Logger.error
        env
        $"Database execution error,
            statement {dbExeError.Statement},
            message {dbExeError.Error.Message}"
    | ErrDbTransactionError dbTranError ->
      Logger.error
        env
        $"Database transaction error,
            step {dbTranError.Step},
            message {dbTranError.Error.Message}"
    | ErrDataReaderCastError rdCastError ->
      Logger.error
        env
        $"Data reader cast error,
            field name {rdCastError.FieldName},
            message {rdCastError.Error.Message}"
    | ErrDataReaderOutOfRangeError rdRangeError ->
      Logger.error
        env
        $"Data reader range error,
            field name {rdRangeError.FieldName},
            message {rdRangeError.Error.Message}"
    | ErrBugSomeThrowException exn ->

      Logger.fatal env $"Exception was throw, msg {exn.Message}, stack trace {exn.StackTrace}"

    | _ ->

    Logger.fatal env $"Error: {error} not matched in Repository error handler"

    RepositoryUnmatchedException($"Error: {error} not matched in Repository error handler")
    |> raise

  let IRepBuilder cache =
    { new IRep<_> with
        member _.Repository =
          { new IRepository<_> with
              member _.Cache = cache } }

  let offices env =
    Logger.debug env "Try get offices from rep"

    cache env
    |> Agent.postAndReply (fun tcs -> CacheCommand.Offices tcs)

  let managers env =
    Logger.debug env "Try get managers from rep"

    cache env
    |> Agent.postAndReply (fun tcs -> CacheCommand.Managers tcs)

  let employers env =
    Logger.debug env "Try get employers from rep"

    cache env
    |> Agent.postAndReply (fun tcs -> CacheCommand.Employers tcs)

  let messages env =
    Logger.debug env "Try get messages from rep"

    cache env
    |> Agent.postAndReply (fun tcs -> CacheCommand.TelegramMessages tcs)

  let deletionItems env =
    Logger.debug env "Try get deletionItems from rep"

    cache env
    |> Agent.postAndReply (fun tcs -> CacheCommand.DeletionItems tcs)

  let tryEmployerByChatId env chatId =
    Logger.debug env $"Try get employer by chatid {chatId} from rep"
    employers env |> Map.tryFind chatId

  let tryManagerByChatId env chatId =
    Logger.debug env $"Try get manager by chatid {chatId} from rep"
    managers env |> Map.tryFind chatId

  let tryMessageByChatId env chatId =
    Logger.debug env $"Try get message by chatid {chatId} from rep"
    messages env |> Map.tryFind chatId

  let tryOfficeByChatId env chatId =
    Logger.debug env $"Try get office by chatid {chatId} from rep"
    offices env |> Map.filter(fun _ office -> office.Manager.ChatId = chatId)

  let tryAddOffice env office =
    let officeDto = OfficeDto.fromDomain office
    let cache = cache env
    Logger.debug env $"Try add new office"

    match Database.insertOffice env officeDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddOffice office
      |> cache.Post

      Logger.info env $"Success add new office {office.OfficeName}"
      true
    | Error err ->

    Logger.warning env $"Error when try add new office {office.OfficeName}"
    errorHandler env err
    false

  let tryAddMessage env message =
    let messageDto = TelegramMessageDto.fromDomain message
    let cache = cache env
    Logger.debug env $"Try add new message {message.Chat.FirstName}"

    match Database.insertTelegramMessage env messageDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddTelegramMessage message
      |> cache.Post

      Logger.info env $"Success add new message {message.Chat.FirstName}"
      true
    | Error err ->

    Logger.warning env $"Error when try add new message {message.Chat.FirstName}"
    errorHandler env err
    false

  let tryAddEmployer env employer =
    let employerDto = EmployerDto.fromDomain employer
    let cache = cache env
    Logger.debug env 
      $"Try add new employer: {employerDto.FirstName} {employerDto.LastName} {employerDto.ChatId}"

    match Database.insertEmployer env employerDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddEmployer employer
      |> cache.Post

      Logger.info env 
        $"Success add new employer: {employerDto.FirstName} {employerDto.LastName} {employerDto.ChatId}"
      true
    | Error err ->

    Logger.warning env 
      $"Error when try add new employer: {employerDto.FirstName} {employerDto.LastName} {employerDto.ChatId}"
    errorHandler env err
    false

  let tryAddManager env manager =
    let managerDto = ManagerDto.fromDomain manager
    let cache = cache env
    Logger.debug env 
      $"Try add new manager: {managerDto.FirstName} {managerDto.LastName} {managerDto.ChatId}"

    match Database.insertManager env managerDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddManager manager
      |> cache.Post

      Logger.info env 
        $"Success add new manager: {managerDto.FirstName} {managerDto.LastName} {managerDto.ChatId}"
      true
    | Error err ->

    Logger.warning env 
      $"Error when try add new manager: {managerDto.FirstName} {managerDto.LastName} {managerDto.ChatId}"
    errorHandler env err
    false

  let tryAddDeletionItem env deletionItem =
    let deletionItemDto = DeletionItemDto.fromDomain deletionItem
    let cache = cache env
    Logger.debug env 
      $"Try add new deletion item: {deletionItemDto.ItemName} {deletionItemDto.OfficeId}"

    match Database.insertDeletionItem env deletionItemDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddDeletionItem deletionItem
      |> cache.Post

      Logger.info env 
        $"Success add new deletion item: {deletionItemDto.ItemName} {deletionItemDto.OfficeId}"
      true
    | Error err ->

    Logger.warning env 
      $"Error when try add new deletion item: {deletionItemDto.ItemName} {deletionItemDto.OfficeId}"
    errorHandler env err
    false

  let tryAddChatId env chatId =
    let chatIdDto = ChatIdDto.fromDomain chatId
    Logger.debug env 
      $"Try add new chatId: {chatIdDto.ChatId}"

    match Database.insertChatId env chatIdDto with
    | Ok _ ->

      Logger.info env 
        $"Success add new chatId: {chatIdDto.ChatId}"
      true
    | Error err ->

    Logger.warning env 
      $"Error when try add new chatId: {chatIdDto.ChatId}"
    errorHandler env err
    false

  let tryDeleteMessage env message =
    let messageDto = TelegramMessageDto.fromDomain message
    let cache = cache env
    Logger.debug env $"Try delete message {message.Chat.FirstName}"

    match Database.deleteTelegramMessage env messageDto with
    | Ok _ ->
      CacheCommand.RemoveTelegramMessage message
      |> cache.Post

      Logger.info env $"Success delete message {message.Chat.FirstName}"
      true
    | Error err ->

    Logger.warning env $"Error when try delete message {message.Chat.FirstName}"
    errorHandler env err
    false

  let tryUpdateMessage env message =
    let messageDto = TelegramMessageDto.fromDomain message
    let cache = cache env
    Logger.debug env $"Try update message {message.Chat.FirstName}"

    match Database.updateTelegramMessage env messageDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddTelegramMessage message
      |> cache.Post

      Logger.info env $"Success update message {message.Chat.FirstName}"
      true
    | Error err ->

    Logger.warning env $"Error when try update message {message.Chat.FirstName}"
    errorHandler env err
    false

  let tryDeleteOffice env office =
    let officeDto = OfficeDto.fromDomain office
    let cache = cache env
    Logger.debug env $"Try delete office"

    match Database.deleteOffice env officeDto with
    | Ok _ ->
      CacheCommand.RemoveOffice office
      |> cache.Post

      Logger.info env $"Success delete office {office.OfficeName}"
      true
    | Error err ->

    Logger.warning env $"Error when try delete office {office.OfficeName}"
    errorHandler env err
    false

  let tryUpdateOffice env office =
    let officeDto = OfficeDto.fromDomain office
    let cache = cache env
    Logger.debug env $"Try update office {office.OfficeName}"

    match Database.updateOffice env officeDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddOffice office
      |> cache.Post

      Logger.info env $"Success update office {office.OfficeName}"
      true
    | Error err ->

    Logger.warning env $"Error when try update office {office.OfficeName}"
    errorHandler env err
    false

  let tryUpdateEmployer env employer =
    let employerDto = EmployerDto.fromDomain employer
    let cache = cache env
    Logger.debug env $"Try update employer {employer.FirstName} {employer.LastName}"

    match Database.updateEmployer env employerDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddEmployer employer
      |> cache.Post

      Logger.info env $"Success update employer {employer.FirstName} {employer.LastName}"
      true
    | Error err ->

    Logger.warning env $"Error when try update employer {employer.FirstName} {employer.LastName}"
    errorHandler env err
    false

  let tryUpdateDeletionItems env deletionItems =
    let deletionItemsDtos = List.map DeletionItemDto.fromDomain deletionItems
    let cache = cache env
    Logger.debug env $"Try update deletion items"

    match Database.updateDeletionItems env deletionItemsDtos with
    | Ok _ ->
      CacheCommand.UpdateOrAddDeletionItems deletionItems
      |> cache.Post

      Logger.info env $"Success update deletion items"
      true
    | Error err ->

    Logger.warning env $"Error when try update deletion items"
    errorHandler env err
    false

  let tryUpdateDeletionItem env deletionItem =
    let deletionItemDto = DeletionItemDto.fromDomain deletionItem
    let cache = cache env
    Logger.debug env $"Try update deletion item with name {deletionItem.Item.Name}"

    match Database.updateDeletionItem env deletionItemDto with
    | Ok _ ->
      CacheCommand.UpdateOrAddDeletionItem deletionItem
      |> cache.Post

      Logger.info env $"Success update deletion item with name {deletionItem.Item.Name}"
      true
    | Error err ->

    Logger.warning env $"Error when try update deletion item with name {deletionItem.Item.Name}"
    errorHandler env err
    false
