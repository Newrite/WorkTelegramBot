namespace WorkTelegram.Core
    
    module Field =
        
        [<Literal>]
        val ChatId: string = "chat_id"
        
        [<Literal>]
        val MessageJson: string = "message_json"
        
        [<Literal>]
        val FirstName: string = "first_name"
        
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
        
        [<Literal>]
        val IsReadyToDeletion: string = "ready_to_deletion"
    
    type ChatIdDto =
        { ChatId: int64 }
    
    module ChatIdDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> ChatIdDto
        
        val fromDomain: chatId: UMX.ChatId -> ChatIdDto
        
        val toDomain: chatIdTable: ChatIdDto -> UMX.ChatId
        
        [<Literal>]
        val TableName: string = "chat_id_table"
    
    type TelegramMessageDto =
        {
          ChatId: int64
          MessageJson: string
        }
    
    module TelegramMessageDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> TelegramMessageDto
        
        val fromDomain: message: Types.TelegramMessage -> TelegramMessageDto
        
        val toDomain: message: TelegramMessageDto -> Types.TelegramMessage
        
        [<Literal>]
        val TableName: string = "message"
    
    type ManagerDto =
        {
          ChatId: int64
          FirstName: string
          LastName: string
        }
    
    module ManagerDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> ManagerDto
        
        val fromDomain: manager: Types.Manager -> ManagerDto
        
        val toDomain: manager: ManagerDto -> Types.Manager
        
        [<Literal>]
        val TableName: string = "manager"
    
    type OfficeDto =
        {
          OfficeId: System.Guid
          OfficeName: string
          IsHidden: bool
          ManagerId: int64
        }
    
    module OfficeDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> OfficeDto
        
        val fromDomain: office: Types.Office -> OfficeDto
        
        val toDomain: office: OfficeDto -> manager: ManagerDto -> Types.Office
        
        val toDomainWithManager:
          office: OfficeDto -> manager: Types.Manager -> Types.Office
        
        [<Literal>]
        val TableName: string = "office"
    
    type EmployerDto =
        {
          ChatId: int64
          FirstName: string
          LastName: string
          IsApproved: bool
          OfficeId: System.Guid
        }
    
    module EmployerDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> EmployerDto
        
        val fromDomain: employer: Types.Employer -> EmployerDto
        
        val toDomain:
          office: OfficeDto ->
            manager: ManagerDto -> employer: EmployerDto -> Types.Employer
        
        val toDomainWithOffice:
          office: Types.Office -> employer: EmployerDto -> Types.Employer
        
        [<Literal>]
        val TableName: string = "employer"
    
    type DeletionItemDto =
        {
          DeletionId: System.Guid
          ItemName: string
          ItemSerial: string option
          ItemMac: string option
          Count: int64
          Date: int64
          IsDeletion: bool
          IsHidden: bool
          ToLocation: string option
          IsReadyToDeletion: bool
          OfficeId: System.Guid
          ChatId: int64
        }
    
    module DeletionItemDto =
        
        val ofDataReader: rd: System.Data.IDataReader -> DeletionItemDto
        
        val fromDomain: item: Types.DeletionItem -> DeletionItemDto
        
        val toDomain:
          item: DeletionItemDto ->
            employer: EmployerDto ->
            office: OfficeDto -> manager: ManagerDto -> Types.DeletionItem
        
        val toDomainWithEmployer:
          item: DeletionItemDto ->
            employer: Types.Employer -> Types.DeletionItem
        
        [<Literal>]
        val TableName: string = "deletion_items"

