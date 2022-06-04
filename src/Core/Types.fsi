namespace WorkTelegram.Core
    
    module UMX =
        
        [<Measure>]
        type private itemname
        
        [<Measure>]
        type private serialnumber
        
        [<Measure>]
        type private macaddress
        
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
        
        type ItemName = FSharp.UMX.string<itemname>
        
        type Serial = FSharp.UMX.string<serialnumber>
        
        type MacAddress = FSharp.UMX.string<macaddress>
        
        type LastName = FSharp.UMX.string<lastname>
        
        type FirstName = FSharp.UMX.string<firstname>
        
        type OfficeName = FSharp.UMX.string<officename>
        
        type Location = FSharp.UMX.string<location>
        
        type ChatId = int64<chatid>
    
    module Types =
        
        [<RequireQualifiedAccess>]
        type BusinessError =
            | NotFoundInDatabase
            | IncorrectMacAddress
            | NumberMostBePositive
            | IncorrectParseResult
        
        [<Struct>]
        and Inter = int32
        
        [<NoComparison; RequireQualifiedAccess>]
        and AppError =
            | DatabaseError of Donald.DbError
            | BusinessError of BusinessError
        
        [<Struct>]
        and PositiveInt =
            private { Value: uint }
            
            member GetValue: uint
        
        and ItemWithSerial =
            {
              Name: UMX.ItemName
              Serial: UMX.Serial
            }
        
        and ItemWithMacAddress =
            {
              Name: UMX.ItemName
              Serial: UMX.Serial
              MacAddress: UMX.MacAddress
            }
        
        and ItemWithOnlyName =
            { Name: UMX.ItemName }
        
        [<RequireQualifiedAccess>]
        and Item =
            | ItemWithSerial of ItemWithSerial
            | ItemWithMacAddress of ItemWithMacAddress
            | ItemWithOnlyName of ItemWithOnlyName
            
            member MacAddress: UMX.MacAddress option
            
            member Name: UMX.ItemName
            
            member Serial: UMX.Serial option
        
        [<NoEquality; NoComparison; RequireQualifiedAccess>]
        and CacheCommand =
            | Initialization of Env
            | EmployerByChatId of
              UMX.ChatId * AsyncReplyChannel<RecordedEmployer option>
            | ManagerByChatId of
              UMX.ChatId * AsyncReplyChannel<RecordedManager option>
            | Offices of AsyncReplyChannel<RecordedOffice list option>
            | AddOffice of RecordedOffice
            | AddEmployer of RecordedEmployer
            | AddManager of RecordedManager
            | CurrentCache of AsyncReplyChannel<Cache>
            | GetOfficeEmployers of
              RecordedOffice * AsyncReplyChannel<RecordedEmployer list option>
            | DeleteOffice of RecordedOffice
        
        and Cache =
            {
              Employers: RecordedEmployer list
              Offices: RecordedOffice list
              Managers: RecordedManager list
            }
        
        [<NoEquality; NoComparison>]
        and Logging =
            {
              Debug: string -> unit
              Info: string -> unit
              Error: string -> unit
              Warning: string -> unit
              Fatal: string -> unit
            }
        
        [<NoEquality; NoComparison>]
        and Env =
            {
              Log: Logging
              Config: Funogram.Types.BotConfig
              DBConn: Microsoft.Data.Sqlite.SqliteConnection
              CacheActor: MailboxProcessor<CacheCommand>
            }
        
        and RecordedManager =
            {
              ChatId: UMX.ChatId
              FirstName: UMX.FirstName
              LastName: UMX.LastName
            }
        
        and RecordedOffice =
            {
              OfficeName: UMX.OfficeName
              Manager: RecordedManager
            }
        
        and RecordedEmployer =
            {
              FirstName: UMX.FirstName
              LastName: UMX.LastName
              Office: RecordedOffice
              ChatId: UMX.ChatId
            }
        
        and RecordedDeletionItem =
            {
              Item: Item
              Count: PositiveInt
              Time: System.DateTime
              Location: UMX.Location option
              Employer: RecordedEmployer
            }
            
            override ToString: unit -> string
        
        val a: i: Inter -> unit
        
        module MacAddress =
            
            val validate:
              input: string -> Result<FSharp.UMX.string<'u>,BusinessError>
        
        module OfficeName =
            
            val equals:
              officeOne: UMX.OfficeName -> officeTwo: UMX.OfficeName -> bool
        
        module PositiveInt =
            
            val create: count: uint32 -> Result<PositiveInt,BusinessError>
            
            val tryParse: str: string -> Result<PositiveInt,BusinessError>
            
            val one: PositiveInt
        
        module ItemWithSerial =
            
            val create:
              name: UMX.ItemName -> serial: UMX.Serial -> ItemWithSerial
        
        module ItemWithMacAddress =
            
            val create:
              name: UMX.ItemName -> serial: UMX.Serial
              -> macaddress: UMX.MacAddress -> ItemWithMacAddress
        
        module ItemWithOnlyName =
            
            val create: name: UMX.ItemName -> ItemWithOnlyName
        
        module Item =
            
            val createWithSerial:
              name: UMX.ItemName -> serial: UMX.Serial -> Item
            
            val createWithMacAddress:
              name: UMX.ItemName -> serial: UMX.Serial
              -> macaddress: UMX.MacAddress -> Item
            
            val createWithOnlyName: name: UMX.ItemName -> Item
            
            val create:
              name: UMX.ItemName -> serial: UMX.Serial option
              -> macaddress: UMX.MacAddress option -> Item
        
        module RecordedEmployer =
            
            val create:
              firstName: UMX.FirstName -> lastName: UMX.LastName
              -> office: RecordedOffice -> chatId: UMX.ChatId
                -> RecordedEmployer

