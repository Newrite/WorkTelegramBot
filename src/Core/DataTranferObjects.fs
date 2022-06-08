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
  let FirstName = "firt_name"

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

type RecordOffice = { OfficeName: string; ManagerChatId: int64 }

type RecordEmployer =
  { FirstName: string
    LastName: string
    ChatId: int64
    OfficeId: int64
    OfficeName: string }

type RecordDeletionItem =
  { ItemName: string
    ItemSerial: string option
    ItemMac: string option
    Count: uint32
    Time: DateTime
    Location: string option
    OfficeId: int64
    EmployerChatId: int64 }

  override self.ToString() =
    let macText =
      if self.ItemMac.IsSome then
        self.ItemMac.Value
      else
        "Нет"

    let serialText =
      if self.ItemSerial.IsSome then
        %self.ItemSerial.Value
      else
        "Нет"

    let locationText =
      if self.Location.IsSome then
        %self.Location.Value
      else
        "Не указано"

    $"""
        Имя позиции    : {self.ItemName}
        Мак адрес      : {macText}
        Серийный номер : {serialText}
        Куда или зачем : {locationText}
        Количество     : {self.Count}
        Дата           : {self.Time}"""

[<RequireQualifiedAccess>]
module Record =

  let createOffice (officeName: OfficeName) (managerChatId: ChatId) =
    { OfficeName = %officeName
      ManagerChatId = %managerChatId }

  let createEmployer
    (officeId: OfficeId)
    (officeName: OfficeName)
    (firstName: FirstName)
    (lastName: LastName)
    (chatId: ChatId)
    =
    { FirstName = %firstName
      LastName = %lastName
      OfficeId = %officeId
      OfficeName = %officeName
      ChatId = %chatId }

  let createDeletionItem
    (item: Item)
    (count: PositiveInt)
    time
    (location: Location option)
    (officeId: OfficeId)
    (employerChatId: ChatId)
    =
    { ItemName = %item.Name
      ItemSerial = Option.map (fun s -> %s) item.Serial
      ItemMac = Option.map (fun (m: MacAddress) -> m.GetValue) item.MacAddress
      Count = count.GetValue
      Time = time
      Location = Option.map (fun l -> %l) location
      OfficeId = %officeId
      EmployerChatId = %employerChatId }

type ChatIdDto = { ChatId: int64 }

[<RequireQualifiedAccess>]
module ChatIdDto =

  let ofDataReader (rd: IDataReader) = { ChatId = rd.ReadInt64 Field.ChatId }

  let fromDomain (chatId: ChatId) = { ChatId = %chatId }

  let toDomain (chatIdTable: ChatIdDto) : ChatId = %chatIdTable.ChatId

  [<Literal>]
  let TableName = "chat_id_table"

type MessageDto = { ChatId: int64; MessageJson: string }

[<RequireQualifiedAccess>]
module MessageDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      MessageJson = rd.ReadString Field.MessageJson }

  let fromDomain (message: Funogram.Telegram.Types.Message) =
    { ChatId = message.Chat.Id
      MessageJson = Json.serialize message }

  let toDomain (message: MessageDto) =
    Json.deserialize<Funogram.Telegram.Types.Message> message.MessageJson

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
  { OfficeId: int64
    OfficeName: string
    IsHidden: bool
    ManagerId: int64 }

[<RequireQualifiedAccess>]
module OfficeDto =

  let ofDataReader (rd: IDataReader) =
    { OfficeId = rd.ReadInt64 Field.OfficeId
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
    OfficeId: int64 }

[<RequireQualifiedAccess>]
module EmployerDto =

  let ofDataReader (rd: IDataReader) =
    { ChatId = rd.ReadInt64 Field.ChatId
      FirstName = rd.ReadString "first_name"
      LastName = rd.ReadString Field.LastName
      IsApproved = rd.ReadBoolean Field.IsApproved
      OfficeId = rd.ReadInt64 Field.OfficeId }

  let fromDomain (employer: Employer) isApproved =
    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      IsApproved = isApproved
      OfficeId = %employer.Office.OfficeId }

  let toDomain (office: OfficeDto) (manager: ManagerDto) (employer: EmployerDto) : Employer =
    let office = OfficeDto.toDomain office manager

    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      Office = office }

  let toDomainWithOffice (office: Office) (employer: EmployerDto) =
    { ChatId = %employer.ChatId
      FirstName = %employer.FirstName
      LastName = %employer.LastName
      Office = office }

  [<Literal>]
  let TableName = "employer"

type DeletionItemDto =
  { DeletionId: int64
    ItemName: string
    ItemSerial: string option
    ItemMac: string option
    Count: int64
    Date: int64
    IsDeletion: bool
    IsHidden: bool
    ToLocation: string option
    OfficeId: int64
    ChatId: int64 }

[<RequireQualifiedAccess>]
module DeletionItemDto =

  let ofDataReader (rd: IDataReader) =
    { DeletionId = rd.ReadInt64 Field.DeletionId
      ItemName = rd.ReadString Field.ItemName
      ItemSerial = rd.ReadStringOption Field.ItemSerial
      ItemMac = rd.ReadStringOption Field.ItemMac
      Count = rd.ReadInt64 Field.Count
      Date = rd.ReadInt64 Field.Date
      IsDeletion = rd.ReadBoolean Field.IsDeletion
      IsHidden = rd.ReadBoolean Field.IsHidden
      ToLocation = rd.ReadStringOption Field.ToLocation
      OfficeId = rd.ReadInt64 Field.OfficeId
      ChatId = rd.ReadInt64 Field.ChatId }

  let fromDomain (item: DeletionItem) =
    { DeletionId = %item.DeletionId
      ItemName = %item.Item.Name
      ItemSerial = Option.map (fun s -> %s) item.Item.Serial
      ItemMac = Option.map (fun (m: MacAddress) -> m.GetValue) item.Item.MacAddress
      Count = item.Count.GetValue |> int64
      Date = item.Time.Ticks
      IsDeletion = item.IsDeletion
      IsHidden = item.IsHidden
      ToLocation = Option.map (fun l -> %l) item.Location
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
      Count = count.Value }

  [<Literal>]
  let TableName = "deletion_items"
