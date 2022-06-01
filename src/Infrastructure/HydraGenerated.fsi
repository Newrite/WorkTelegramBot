namespace WorkTelegram.HydraGenerated
    
    type Column =
        
        new: reader: System.Data.IDataReader * getOrdinal: (string -> int) *
             column: string -> Column
        
        member IsNull: unit -> bool
        
        override ToString: unit -> string
        
        member Name: string
    
    type RequiredColumn<'T,'Reader when 'Reader :> System.Data.IDataReader> =
        inherit Column
        
        new: reader: 'Reader * getOrdinal: (string -> int) * getter: (int -> 'T) *
             column: string -> RequiredColumn<'T,'Reader>
        
        member Read: ?alias: string -> 'T
    
    type OptionalColumn<'T,'Reader when 'Reader :> System.Data.IDataReader> =
        inherit Column
        
        new: reader: 'Reader * getOrdinal: (string -> int) * getter: (int -> 'T) *
             column: string -> OptionalColumn<'T,'Reader>
        
        member Read: ?alias: string -> 'T option
    
    type RequiredBinaryColumn<'T,'Reader when 'Reader :> System.Data.IDataReader> =
        inherit Column
        
        new: reader: 'Reader * getOrdinal: (string -> int) *
             getValue: (int -> obj) * column: string
               -> RequiredBinaryColumn<'T,'Reader>
        
        member Read: ?alias: string -> byte[]
    
    type OptionalBinaryColumn<'T,'Reader when 'Reader :> System.Data.IDataReader> =
        inherit Column
        
        new: reader: 'Reader * getOrdinal: (string -> int) *
             getValue: (int -> obj) * column: string
               -> OptionalBinaryColumn<'T,'Reader>
        
        member Read: ?alias: string -> byte[] option
    
    module main =
        
        [<CLIMutable>]
        type chat_id_table =
            { chat_id: int64 }
        
        type chat_id_tableReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> chat_id_tableReader
            
            member Read: unit -> chat_id_table
            
            member ReadIfNotNull: unit -> chat_id_table option
            
            member
              chat_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
        
        [<CLIMutable>]
        type deletion_items =
            {
              deletion_id: int64
              item_name: string
              item_serial: Option<string>
              item_mac: Option<string>
              count: int64
              date: int64
              is_deletion: bool
              is_hidden: bool
              to_location: Option<string>
              office_id: int64
              chat_id: int64
            }
        
        type deletion_itemsReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> deletion_itemsReader
            
            member Read: unit -> deletion_items
            
            member ReadIfNotNull: unit -> deletion_items option
            
            member
              chat_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member count: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member date: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              deletion_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              is_deletion: RequiredColumn<bool,System.Data.Common.DbDataReader>
            
            member
              is_hidden: RequiredColumn<bool,System.Data.Common.DbDataReader>
            
            member
              item_mac: OptionalColumn<string,System.Data.Common.DbDataReader>
            
            member
              item_name: RequiredColumn<string,System.Data.Common.DbDataReader>
            
            member
              item_serial: OptionalColumn<string,System.Data.Common.DbDataReader>
            
            member
              office_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              to_location: OptionalColumn<string,System.Data.Common.DbDataReader>
        
        [<CLIMutable>]
        type employer =
            {
              chat_id: int64
              first_name: string
              last_name: string
              is_approved: bool
              office_id: int64
            }
        
        type employerReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> employerReader
            
            member Read: unit -> employer
            
            member ReadIfNotNull: unit -> employer option
            
            member
              chat_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              first_name: RequiredColumn<string,System.Data.Common.DbDataReader>
            
            member
              is_approved: RequiredColumn<bool,System.Data.Common.DbDataReader>
            
            member
              last_name: RequiredColumn<string,System.Data.Common.DbDataReader>
            
            member
              office_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
        
        [<CLIMutable>]
        type manager =
            {
              chat_id: int64
              firt_name: string
              last_name: string
            }
        
        type managerReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> managerReader
            
            member Read: unit -> manager
            
            member ReadIfNotNull: unit -> manager option
            
            member
              chat_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              firt_name: RequiredColumn<string,System.Data.Common.DbDataReader>
            
            member
              last_name: RequiredColumn<string,System.Data.Common.DbDataReader>
        
        [<CLIMutable>]
        type message =
            {
              chat_id: int64
              message_json: string
            }
        
        type messageReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> messageReader
            
            member Read: unit -> message
            
            member ReadIfNotNull: unit -> message option
            
            member
              chat_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              message_json: RequiredColumn<string,
                                           System.Data.Common.DbDataReader>
        
        [<CLIMutable>]
        type office =
            {
              office_id: int64
              office_name: string
              is_hidden: bool
              manager_id: int64
            }
        
        type officeReader =
            
            new: reader: System.Data.Common.DbDataReader *
                 getOrdinal: (string -> int) -> officeReader
            
            member Read: unit -> office
            
            member ReadIfNotNull: unit -> office option
            
            member
              is_hidden: RequiredColumn<bool,System.Data.Common.DbDataReader>
            
            member
              manager_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              office_id: RequiredColumn<int64,System.Data.Common.DbDataReader>
            
            member
              office_name: RequiredColumn<string,System.Data.Common.DbDataReader>
    
    type HydraReader =
        
        new: reader: System.Data.Common.DbDataReader -> HydraReader
        
        static member
          private GetPrimitiveReader: t: System.Type *
                                      reader: System.Data.Common.DbDataReader *
                                      isOpt: bool -> (int -> obj) option
        
        static member
          Read: reader: System.Data.Common.DbDataReader -> (unit -> 'T)
        
        member
          private GetReaderByName: entity: string * isOption: bool
                                     -> (unit -> obj)
        
        member private AccFieldCount: int
        
        member ``main.chat_id_table`` : main.chat_id_tableReader
        
        member ``main.deletion_items`` : main.deletion_itemsReader
        
        member ``main.employer`` : main.employerReader
        
        member ``main.manager`` : main.managerReader
        
        member ``main.message`` : main.messageReader
        
        member ``main.office`` : main.officeReader

