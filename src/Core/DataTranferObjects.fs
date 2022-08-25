namespace WorkTelegram.Core

open System.Data
open Donald
open FSharp.UMX
open FSharp.Json
open System
open WorkTelegram.Core

module Field =
  [<Literal>]
  let ChatId = "chat_id"

  [<Literal>]
  let MessageJson = "message_json"

  [<Literal>]
  let FirstName = "first_name"

  [<Literal>]
  let LastName = "last_name"

  [<Literal>]
  let OfficeId = "office_id"

  [<Literal>]
  let OfficeName = "office_name"

  [<Literal>]
  let IsHidden = "is_hidden"

  [<Literal>]
  let ManagerId = "manager_id"

  [<Literal>]
  let IsApproved = "is_approved"

  [<Literal>]
  let DeletionId = "deletion_id"

  [<Literal>]
  let ItemName = "item_name"

  [<Literal>]
  let ItemSerial = "item_serial"

  [<Literal>]
  let ItemMac = "item_mac"

  [<Literal>]
  let Count = "count"

  [<Literal>]
  let Date = "date"

  [<Literal>]
  let IsDeletion = "is_deletion"

  [<Literal>]
  let ToLocation = "to_location"

  [<Literal>]
  let IsReadyToDeletion = "ready_to_deletion"

type ChatIdDto = { ChatId: int64 }


[<RequireQualifiedAccess>]
module ChatIdDto =

  let ofDataReader (rd: IDataReader) = { ChatId = rd.ReadInt64 Field.ChatId }

  let fromDomain (chatId: ChatId) = { ChatId = %chatId }

  let toDomain (chatIdTable: ChatIdDto) : ChatId = %chatIdTable.ChatId

  [<Literal>]
  let TableName = "chat_id_table"

type TelegramMessageDto = { ChatId: int64; MessageJson: string }

[<RequireQualifiedAccess>]
module TelegramMessageDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      MessageJson = rd.ReadString Field.MessageJson }

  let fromDomain (message: TelegramMessage) =
    { ChatId = message.Chat.Id
      MessageJson = Json.serialize message }

  let toDomain (message: TelegramMessageDto) =
    Json.deserialize<TelegramMessage> message.MessageJson

  [<Literal>]
  let TableName = "message"

type ManagerDto =
  { ChatId: int64
    FirstName: string
    LastName: string }


[<RequireQualifiedAccess>]
module ManagerDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString Field.FirstName
      LastName = rd.ReadString Field.LastName }

  let fromDomain (manager: Manager) : ManagerDto =
    { ChatId = %manager.ChatId
      FirstName = %manager.FirstName
      LastName = %manager.LastName }

  let toDomain (manager: ManagerDto) : Manager =
    { ChatId = %manager.ChatId
      FirstName = %manager.FirstName
      LastName = %manager.LastName }

  [<Literal>]
  let TableName = "manager"

type OfficeDto =
  { OfficeId: Guid
    OfficeName: string
    IsHidden: bool
    ManagerId: int64 }

[<RequireQualifiedAccess>]
module OfficeDto =

  let ofDataReader (rd: IDataReader) =
    { OfficeId = rd.ReadGuid Field.OfficeId
      OfficeName = rd.ReadString Field.OfficeName
      IsHidden = rd.ReadBoolean Field.IsHidden
      ManagerId = rd.ReadInt64 Field.ManagerId }

  let fromDomain (office: Office) : OfficeDto =
    { OfficeId = %office.OfficeId
      OfficeName = %office.OfficeName
      IsHidden = true
      ManagerId = %office.Manager.ChatId }

  let toDomain (office: OfficeDto) (manager: ManagerDto) =

    let manager = ManagerDto.toDomain manager

    { OfficeId = %office.OfficeId
      IsHidden = office.IsHidden
      OfficeName = %office.OfficeName
      Manager = manager }

  let toDomainWithManager (office: OfficeDto) manager =
    { OfficeId = %office.OfficeId
      IsHidden = office.IsHidden
      OfficeName = %office.OfficeName
      Manager = manager }

  [<Literal>]
  let TableName = "office"

type EmployerDto =
  { ChatId: int64
    FirstName: string
    LastName: string
    IsApproved: bool
    OfficeId: Guid }

[<RequireQualifiedAccess>]
module EmployerDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString Field.FirstName
      LastName = rd.ReadString Field.LastName
      IsApproved = rd.ReadBoolean Field.IsApproved
      OfficeId = rd.ReadGuid Field.OfficeId }

  let fromDomain (employer: Employer) =
    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      IsApproved = employer.IsApproved
      OfficeId = %employer.Office.OfficeId }

  let toDomain (office: OfficeDto) (manager: ManagerDto) (employer: EmployerDto) : Employer =
    let office = OfficeDto.toDomain office manager

    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      Office = office
      IsApproved = employer.IsApproved }

  let toDomainWithOffice (office: Office) (employer: EmployerDto) =
    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      Office = office
      IsApproved = employer.IsApproved }

  [<Literal>]
  let TableName = "employer"

type DeletionItemDto =
  { DeletionId: Guid
    ItemName: string
    ItemSerial: string option
    ItemMac: string option
    Count: int64
    Date: int64
    IsDeletion: bool
    IsHidden: bool
    ToLocation: string option
    IsReadyToDeletion: bool
    OfficeId: Guid
    ChatId: int64 }

[<RequireQualifiedAccess>]
module DeletionItemDto =

  let ofDataReader (rd: IDataReader) =
    { DeletionId = rd.ReadGuid Field.DeletionId
      ItemName = rd.ReadString Field.ItemName
      ItemSerial = rd.ReadStringOption Field.ItemSerial
      ItemMac = rd.ReadStringOption Field.ItemMac
      Count = rd.ReadInt64 Field.Count
      Date = rd.ReadInt64 Field.Date
      IsDeletion = rd.ReadBoolean Field.IsDeletion
      IsHidden = rd.ReadBoolean Field.IsHidden
      ToLocation = rd.ReadStringOption Field.ToLocation
      IsReadyToDeletion = rd.ReadBoolean Field.IsReadyToDeletion
      OfficeId = rd.ReadGuid Field.OfficeId
      ChatId = rd.ReadInt64 Field.ChatId }

  let fromDomain (item: DeletionItem) =
    { DeletionId = %item.DeletionId
      ItemName = %item.Item.Name
      ItemSerial = Option.map (fun s -> %s) item.Item.Serial
      ItemMac = Option.map (fun (m: MacAddress) -> m.Value) item.Item.MacAddress
      Count = item.Count.Value |> int64
      Date = item.Time.Ticks
      IsDeletion = item.IsDeletion
      IsHidden = item.IsHidden
      ToLocation = Option.map (fun l -> %l) item.Location
      IsReadyToDeletion = item.IsReadyToDeletion
      OfficeId = %item.Employer.Office.OfficeId
      ChatId = %item.Employer.ChatId }

  let toDomain
    (item: DeletionItemDto)
    (employer: EmployerDto)
    (office: OfficeDto)
    (manager: ManagerDto)
    : DeletionItem =
    let employer = EmployerDto.toDomain office manager employer

    let itemRecorded =
      let serial = Option.map (fun s -> %s) item.ItemSerial
      let macaddress = MacAddress.fromOptionString item.ItemMac
      Item.create %item.ItemName serial macaddress

    let count =
      item.Count
      |> uint
      |> PositiveInt.create
      |> Option.ofResult

    { DeletionId = %item.DeletionId
      Item = itemRecorded
      IsDeletion = item.IsDeletion
      IsHidden = item.IsHidden
      Employer = employer
      Time = DateTime.FromBinary(item.Date)
      Location = Option.map (fun l -> %l) item.ToLocation
      IsReadyToDeletion = item.IsReadyToDeletion
      Count = count.Value }

  let toDomainWithEmployer (item: DeletionItemDto) (employer: Employer) =

    let itemRecorded =
      let serial = Option.map (fun s -> %s) item.ItemSerial
      let macaddress = MacAddress.fromOptionString item.ItemMac
      Item.create %item.ItemName serial macaddress

    let count =
      item.Count
      |> uint
      |> PositiveInt.create
      |> Option.ofResult

    { DeletionId = %item.DeletionId
      Item = itemRecorded
      IsDeletion = item.IsDeletion
      IsHidden = item.IsHidden
      Employer = employer
      Time = DateTime.FromBinary(item.Date)
      Location = Option.map (fun l -> %l) item.ToLocation
      IsReadyToDeletion = item.IsReadyToDeletion
      Count = count.Value }

  [<Literal>]
  let TableName = "deletion_items"
