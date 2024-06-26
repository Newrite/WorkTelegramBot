namespace WorkTelegram.Core
    
    [<AutoOpen>]
    module UMX =
        
        [<Measure>]
        type private itemname
        
        [<Measure>]
        type private serialnumber
        
        [<Measure>]
        type private lastname
        
        [<Measure>]
        type private firstname
        
        [<Measure>]
        type private officename
        
        [<Measure>]
        type private location
        
        [<Measure>]
        type private chatid
        
        [<Measure>]
        type private officeid
        
        [<Measure>]
        type private deletionid
        
        type ItemName = FSharp.UMX.string<itemname>
        
        type Serial = FSharp.UMX.string<serialnumber>
        
        type LastName = FSharp.UMX.string<lastname>
        
        type FirstName = FSharp.UMX.string<firstname>
        
        type OfficeName = FSharp.UMX.string<officename>
        
        type Location = FSharp.UMX.string<location>
        
        type ChatId = int64<chatid>
        
        type OfficeId = FSharp.UMX.Guid<officeid>
        
        type DeletionId = FSharp.UMX.Guid<deletionid>
    
    [<AutoOpen>]
    module Types =
        
        type TelegramMessage = Funogram.Telegram.Types.Message
        
        [<NoComparison; RequireQualifiedAccess>]
        type BusinessError =
            | NotFoundInDatabase of SearchedType: System.Type
            | IncorrectMacAddress of PassedIncorrectValue: string
            | NumberMustBePositive of PassedIncorrectNumber: uint32
            | IncorrectParsePositiveNumber of
              PassedIncorrectStringToParse: string
        
        [<NoComparison; RequireQualifiedAccess>]
        type AppError =
            | DatabaseError of Donald.DbError
            | BusinessError of BusinessError
            | Bug of System.Exception
        
        module ErrorPatterns =
            
            val (|ErrBugSomeThrowException|_|) :
              error: AppError -> System.Exception option
            
            [<AutoOpen>]
            module DatabasePatterns =
                
                val (|ErrDataReaderOutOfRangeError|_|) :
                  error: AppError -> Donald.DataReaderOutOfRangeError option
                
                val (|ErrDataReaderCastError|_|) :
                  error: AppError -> Donald.DataReaderCastError option
                
                val (|ErrDbConnectionError|_|) :
                  error: AppError -> Donald.DbConnectionError option
                
                val (|ErrDbTransactionError|_|) :
                  error: AppError -> Donald.DbTransactionError option
                
                val (|ErrDbExecutionError|_|) :
                  error: AppError -> Donald.DbExecutionError option
            
            [<AutoOpen>]
            module BusinessPatterns =
                
                val (|ErrNotFoundInDatabase|_|) :
                  error: AppError -> System.Type option
                
                val (|ErrIncorrectMacAddress|_|) :
                  error: AppError -> string option
                
                val (|ErrNumberMustBePositive|_|) :
                  error: AppError -> uint32 option
                
                val (|ErrIncorrectParsePositiveNumber|_|) :
                  error: AppError -> string option
        
        [<Struct>]
        type MacAddress =
            private { MacAddress: string }
            
            override ToString: unit -> string
            
            member Value: string
        
        [<RequireQualifiedAccess>]
        module MacAddress =
            
            val fromString: input: string -> Result<MacAddress,BusinessError>
            
            val fromOptionString: input: string option -> MacAddress option
        
        [<RequireQualifiedAccess>]
        module OfficeName =
            
            val equals:
              officeOne: UMX.OfficeName -> officeTwo: UMX.OfficeName -> bool
        
        [<Struct>]
        type PositiveInt =
            private { PositiveInt: uint }
            
            member Value: uint
        
        [<RequireQualifiedAccess>]
        module PositiveInt =
            
            val create: count: uint32 -> Result<PositiveInt,BusinessError>
            
            val tryParse: str: string -> Result<PositiveInt,BusinessError>
            
            val one: PositiveInt
        
        type ItemWithSerial =
            {
              Name: UMX.ItemName
              Serial: UMX.Serial
            }
        
        [<RequireQualifiedAccess>]
        module ItemWithSerial =
            
            val create:
              name: UMX.ItemName -> serial: UMX.Serial -> ItemWithSerial
        
        type ItemWithMacAddress =
            {
              Name: UMX.ItemName
              Serial: UMX.Serial
              MacAddress: MacAddress
            }
        
        [<RequireQualifiedAccess>]
        module ItemWithMacAddress =
            
            val create:
              name: UMX.ItemName ->
                serial: UMX.Serial ->
                macaddress: MacAddress -> ItemWithMacAddress
        
        type ItemWithOnlyName =
            { Name: UMX.ItemName }
        
        [<RequireQualifiedAccess>]
        module ItemWithOnlyName =
            
            val create: name: UMX.ItemName -> ItemWithOnlyName
        
        [<RequireQualifiedAccess>]
        type Item =
            | ItemWithSerial of ItemWithSerial
            | ItemWithMacAddress of ItemWithMacAddress
            | ItemWithOnlyName of ItemWithOnlyName
            
            member MacAddress: MacAddress option
            
            member Name: UMX.ItemName
            
            member Serial: UMX.Serial option
        
        [<RequireQualifiedAccess>]
        module Item =
            
            val createWithSerial:
              name: UMX.ItemName -> serial: UMX.Serial -> Item
            
            val createWithMacAddress:
              name: UMX.ItemName ->
                serial: UMX.Serial -> macaddress: MacAddress -> Item
            
            val createWithOnlyName: name: UMX.ItemName -> Item
            
            val create:
              name: UMX.ItemName ->
                serial: UMX.Serial option ->
                macaddress: MacAddress option -> Item
        
        type Manager =
            {
              ChatId: UMX.ChatId
              FirstName: UMX.FirstName
              LastName: UMX.LastName
            }
        
        type Office =
            {
              OfficeId: UMX.OfficeId
              IsHidden: bool
              OfficeName: UMX.OfficeName
              Manager: Manager
            }
        
        [<RequireQualifiedAccess>]
        module Office =
            
            val create: officeName: UMX.OfficeName -> manager: Manager -> Office
        
        type Employer =
            {
              FirstName: UMX.FirstName
              LastName: UMX.LastName
              Office: Office
              ChatId: UMX.ChatId
              IsApproved: bool
            }
        
        [<RequireQualifiedAccess>]
        module Employer =
            
            val create:
              firstName: UMX.FirstName ->
                lastName: UMX.LastName ->
                office: Office -> chatId: UMX.ChatId -> Employer
        
        [<RequireQualifiedAccess>]
        module Manager =
            
            val create:
              chatId: UMX.ChatId ->
                firstName: UMX.FirstName -> lastName: UMX.LastName -> Manager
            
            val asEmployer: manager: Manager -> office: Office -> Employer
        
        type DeletionItem =
            {
              DeletionId: UMX.DeletionId
              Item: Item
              Count: PositiveInt
              Time: System.DateTime
              IsDeletion: bool
              IsHidden: bool
              Location: UMX.Location option
              IsReadyToDeletion: bool
              Employer: Employer
            }
            
            member Inspired: unit -> bool
            
            override ToString: unit -> string
        
        [<RequireQualifiedAccess>]
        module DeletionItem =
            
            val create:
              item: Item ->
                count: PositiveInt ->
                location: UMX.Location option ->
                employer: Employer -> DeletionItem
            
            val createExcelTableFromItemsAsByte:
              items: DeletionItem seq -> byte array
            
            val inspiredItem:
              currentTime: System.DateTime -> item: DeletionItem -> bool
            
            val readyToDeletionItem: item: DeletionItem -> bool
            
            val hiddenItem: item: DeletionItem -> bool
            
            val notHiddenItem: item: DeletionItem -> bool
            
            val itemToDeletion:
              currentTime: System.DateTime -> item: DeletionItem -> bool

