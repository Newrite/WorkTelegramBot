namespace WorkTelegram.Infrastructure

open System.Threading.Tasks

open WorkTelegram.Core
open FSharp.UMX
open WorkTelegram.Infrastructure

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type CacheCommand =
  | Offices of TaskCompletionSource<OfficesMap>
  | DeletionItems of TaskCompletionSource<DeletionItemsMap>
  | Employers of TaskCompletionSource<EmployersMap>
  | Managers of TaskCompletionSource<ManagersMap>
  | TelegramMessages of TaskCompletionSource<MessagesMap>

  | RemoveOffice of Office
  | RemoveTelegramMessage of TelegramMessage

  | UpdateOrAddOffice of Office
  | UpdateOrAddEmployer of Employer
  | UpdateOrAddManager of Manager
  | UpdateOrAddDeletionItem of DeletionItem
  | UpdateOrAddDeletionItems  of DeletionItem list
  | UpdateOrAddTelegramMessage of TelegramMessage

[<NoComparison>]
type Cache =
  { mutable Employers: EmployersMap
    mutable Offices: OfficesMap
    mutable Managers: ManagersMap
    mutable DeletionItems: DeletionItemsMap
    mutable Messages: MessagesMap }

module Cache =

  let initializationCache logger database errorHandler =

    let inline handleSelect result =
      match result with
      | Ok list -> list
      | Error err ->

      errorHandler logger err
      []

    let employersDto = Database.selectEmployers database |> handleSelect

    let managersDto = Database.selectManagers database |> handleSelect

    let officesDto = Database.selectOffices database |> handleSelect

    let deletionItemsDto =
      Database.selectDeletionItems database
      |> handleSelect

    let messagesDto =
      Database.selectTelegramMessages database
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

  let cacheAgent (cache: Cache) =

    let cacheHandler (msg: CacheCommand) =
      task {

        match msg with
        | CacheCommand.Offices tcs -> tcs.SetResult cache.Offices
        | CacheCommand.Employers tcs -> tcs.SetResult cache.Employers
        | CacheCommand.Managers tcs -> tcs.SetResult cache.Managers
        | CacheCommand.TelegramMessages tcs -> tcs.SetResult cache.Messages
        | CacheCommand.DeletionItems tcs -> tcs.SetResult cache.DeletionItems

        | CacheCommand.RemoveOffice office ->
          cache.Offices <- Map.remove office.OfficeId cache.Offices
        | CacheCommand.RemoveTelegramMessage message ->
          cache.Messages <- Map.remove %message.Chat.Id cache.Messages

        | CacheCommand.UpdateOrAddOffice office ->
          cache.Offices <- Map.add office.OfficeId office cache.Offices
        | CacheCommand.UpdateOrAddDeletionItem item ->
          cache.DeletionItems <- Map.add item.DeletionId item cache.DeletionItems
        | CacheCommand.UpdateOrAddDeletionItems items ->
          for item in items do
            cache.DeletionItems <- Map.add item.DeletionId item cache.DeletionItems
        | CacheCommand.UpdateOrAddEmployer employer ->
          cache.Employers <- Map.add employer.ChatId employer cache.Employers
        | CacheCommand.UpdateOrAddManager manager ->
          cache.Managers <- Map.add manager.ChatId manager cache.Managers
        | CacheCommand.UpdateOrAddTelegramMessage message ->
          cache.Messages <- Map.add %message.Chat.Id message cache.Messages
      }

    cacheHandler
