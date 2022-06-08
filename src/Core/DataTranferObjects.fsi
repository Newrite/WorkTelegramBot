namespace WorkTelegram.Core

module Field =

    [<Literal>]
    val ChatId: string = "chat_id"

    [<Literal>]
    val MessageJson: string = "message_json"

    [<Literal>]
    val FirstName: string = "firt_name"

    [<Literal>]
    val LastName: string = "last_name"

    [<Literal>]
    val OfficeId: string = "office_id"

    [<Literal>]
    val OfficeName: string = "office_name"

    [<Literal>]
    val IsHidden: string = "is_hidden"

    [<Literal>]
    val ManagerId: string = "manager_id"

    [<Literal>]
    val IsApproved: string = "is_approved"

    [<Literal>]
    val DeletionId: string = "deletion_id"

    [<Literal>]
    val ItemName: string = "item_name"

    [<Literal>]
    val ItemSerial: string = "item_serial"

    [<Literal>]
    val ItemMac: string = "item_mac"

    [<Literal>]
    val Count: string = "count"

    [<Literal>]
    val Date: string = "date"

    [<Literal>]
    val IsDeletion: string = "is_deletion"

    [<Literal>]
    val ToLocation: string = "to_location"

type RecordOffice =
    { OfficeName: string
      ManagerChatId: int64 }

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
      Time: System.DateTime
      Location: string option
      OfficeId: int64
      EmployerChatId: int64 }

module Record =

    val createOffice: officeName: UMX.OfficeName -> managerChatId: UMX.ChatId -> RecordOffice

    val createEmployer:
        officeId: UMX.OfficeId ->
        officeName: UMX.OfficeName ->
        firstName: UMX.FirstName ->
        lastName: UMX.LastName ->
        chatId: UMX.ChatId ->
            RecordEmployer

    val createDeletionItem:
        item: Types.Item ->
        count: Types.PositiveInt ->
        time: System.DateTime ->
        location: UMX.Location option ->
        officeId: UMX.OfficeId ->
        employerChatId: UMX.ChatId ->
            RecordDeletionItem

type ChatIdDto = { ChatId: int64 }

module ChatIdDto =

    val ofDataReader: rd: System.Data.IDataReader -> ChatIdDto

    val fromDomain: chatId: UMX.ChatId -> ChatIdDto

    val toDomain: chatIdTable: ChatIdDto -> UMX.ChatId

    [<Literal>]
    val TableName: string = "chat_id"

type MessageDto = { ChatId: int64; MessageJson: string }

module MessageDto =

    val ofDataReader: rd: System.Data.IDataReader -> MessageDto

    val fromDomain: message: Funogram.Telegram.Types.Message -> MessageDto

    val toDomain: message: MessageDto -> Funogram.Telegram.Types.Message

    [<Literal>]
    val TableName: string = "message"

type ManagerDto =
    { ChatId: int64
      FirstName: string
      LastName: string }

module ManagerDto =

    val ofDataReader: rd: System.Data.IDataReader -> ManagerDto

    val fromDomain: manager: Types.Manager -> ManagerDto

    val toDomain: manager: ManagerDto -> Types.Manager

    [<Literal>]
    val TableName: string = "manager"

type OfficeDto =
    { OfficeId: int64
      OfficeName: string
      IsHidden: bool
      ManagerId: int64 }

module OfficeDto =

    val ofDataReader: rd: System.Data.IDataReader -> OfficeDto

    val fromDomain: office: Types.Office -> OfficeDto

    val toDomain: office: OfficeDto -> manager: ManagerDto -> Types.Office

    val toDomainWithManager: office: OfficeDto -> manager: Types.Manager -> Types.Office

    [<Literal>]
    val TableName: string = "office"

type EmployerDto =
    { ChatId: int64
      FirstName: string
      LastName: string
      IsApproved: bool
      OfficeId: int64 }

module EmployerDto =

    val ofDataReader: rd: System.Data.IDataReader -> EmployerDto

    val fromDomain: employer: Types.Employer -> isApproved: bool -> EmployerDto

    val toDomain: office: OfficeDto -> manager: ManagerDto -> employer: EmployerDto -> Types.Employer

    val toDomainWithOffice: office: Types.Office -> employer: EmployerDto -> Types.Employer

    [<Literal>]
    val TableName: string = "employer"

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

module DeletionItemDto =

    val ofDataReader: rd: System.Data.IDataReader -> DeletionItemDto

    val fromDomain: item: Types.DeletionItem -> DeletionItemDto

    val toDomain:
        item: DeletionItemDto -> employer: EmployerDto -> office: OfficeDto -> manager: ManagerDto -> Types.DeletionItem

    val toDomainWithEmployer: item: DeletionItemDto -> employer: Types.Employer -> Types.DeletionItem

    [<Literal>]
    val TableName: string = "deletion_items"
