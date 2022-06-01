namespace WorkTelegram.Infrastructure
    
    module Database =
        
        [<Literal>]
        val private Schema: string
          =
          "CREATE TABLE IF NOT EXISTS chat_id_table (
        chat_id INTEGER NOT NULL PRIMARY KEY
      );

      CREATE TABLE IF NOT EXISTS message (
        chat_id INTEGER NOT NULL PRIMARY KEY,
        message_json TEXT NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );
    
      CREATE TABLE IF NOT EXISTS manager (
        chat_id INTEGER NOT NULL PRIMARY KEY,
        firt_name TEXT NOT NULL,
        last_name TEXT NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );
    
      CREATE TABLE IF NOT EXISTS office (
        office_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        office_name TEXT NOT NULL UNIQUE,
        is_hidden BOOL NOT NULL,
        manager_id INTEGER NOT NULL,
        FOREIGN KEY(manager_id) REFERENCES manager(chat_id)
      );

      CREATE TABLE IF NOT EXISTS employer (
	      chat_id INTEGER NOT NULL PRIMARY KEY,
	      first_name TEXT NOT NULL,
	      last_name TEXT NOT NULL,
        is_approved BOOL NOT NULL,
        office_id INTEGER NOT NULL,
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id),
	      FOREIGN KEY(office_id) REFERENCES office(office_id)
      );
      
      CREATE TABLE IF NOT EXISTS deletion_items (
        deletion_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        item_name TEXT NOT NULL,
        item_serial TEXT DEFAULT(NULL),
        item_mac TEXT DEFAULT(NULL),
        count INTEGER NOT NULL,
        date INTEGER NOT NULL,
        is_deletion BOOL NOT NULL,
        is_hidden BOOL NOT NULL,
        to_location TEXT DEFAULT(NULL),
        office_id INTEGER NOT NULL,
        chat_id INTEGER NOT NULL,
        FOREIGN KEY(office_id) REFERENCES office(office_id),
        FOREIGN KEY(chat_id) REFERENCES chat_id_table(chat_id)
      );"
        
        val private employers:
          SqlHydra.Query.QuerySource<HydraGenerated.main.employer>
        
        val private offices:
          SqlHydra.Query.QuerySource<HydraGenerated.main.office>
        
        val private managers:
          SqlHydra.Query.QuerySource<HydraGenerated.main.manager>
        
        val private deletionItems:
          SqlHydra.Query.QuerySource<HydraGenerated.main.deletion_items>
        
        val private chatIds:
          SqlHydra.Query.QuerySource<HydraGenerated.main.chat_id_table>
        
        val private messages:
          SqlHydra.Query.QuerySource<HydraGenerated.main.message>
        
        val private sharedQueryContext:
          env: Core.Types.Env -> SqlHydra.Query.SelectBuilders.ContextType
        
        val private createQueryContext:
          env: Core.Types.Env -> SqlHydra.Query.SelectBuilders.ContextType
        
        val createConnection:
          log: Core.Types.Logging -> databaseName: string
            -> Microsoft.Data.Sqlite.SqliteConnection
        
        val initalizationTables: env: Core.Types.Env -> unit
        
        val private querySelectOfficeIdByOfficeName:
          officeName: Core.UMX.OfficeName -> SqlHydra.Query.SelectQuery<int64>
        
        val insertMessageAsync:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
            -> System.Threading.Tasks.Task<unit>
        
        val insertMessage:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
            -> unit
        
        val selectMessagesAsync:
          env: Core.Types.Env
            -> System.Threading.Tasks.Task<Funogram.Telegram.Types.Message list>
        
        val selectMessages:
          env: Core.Types.Env -> Funogram.Telegram.Types.Message list
        
        val deleteMessageJsonAsync:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
            -> System.Threading.Tasks.Task<unit>
        
        val deleteMessageJson:
          env: Core.Types.Env -> message: Funogram.Telegram.Types.Message
            -> unit
        
        val insertChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Result<Core.UMX.ChatId,
                                                  Core.Types.DatabaseError>>
        
        val insertChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Result<Core.UMX.ChatId,Core.Types.DatabaseError>
        
        /// Insert manager in database and update managers list in cache
        val insertManagerAsync:
          env: Core.Types.Env -> managerToAdd: Core.Types.RecordedManager
            -> System.Threading.Tasks.Task<Result<Core.Types.RecordedManager,
                                                  string>>
        
        val insertManager:
          env: Core.Types.Env -> managerToAdd: Core.Types.RecordedManager
            -> Result<Core.Types.RecordedManager,string>
        
        val selectManagerByChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.RecordedManager option>
        
        val selectManagerByChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Core.Types.RecordedManager option
        
        val insertEmployerAsync:
          env: Core.Types.Env -> employerToAdd: Core.Types.RecordedEmployer
          -> isApproved: bool
            -> System.Threading.Tasks.Task<Result<Core.Types.RecordedEmployer,
                                                  string>>
        
        val insertEmployer:
          env: Core.Types.Env -> employerToAdd: Core.Types.RecordedEmployer
          -> isApproved: bool -> Result<Core.Types.RecordedEmployer,string>
        
        val selectEmployerByChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.RecordedEmployer option>
        
        val selectEmployerByChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Core.Types.RecordedEmployer option
        
        /// Insert office in database and update office list in cache
        val insertOfficeAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<Result<Core.Types.RecordedOffice,
                                                  string>>
        
        /// Insert office in database and update office list in cache
        val insertOffice:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> Result<Core.Types.RecordedOffice,string>
        
        val selectOfficesByManagerChatIdAsync:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> System.Threading.Tasks.Task<Core.Types.RecordedOffice list>
        
        val selectOfficesByManagerChatId:
          env: Core.Types.Env -> chatId: Core.UMX.ChatId
            -> Core.Types.RecordedOffice list
        
        val insertDeletionItemAsync:
          env: Core.Types.Env -> itemToAdd: Core.Types.RecordedDeletionItem
            -> System.Threading.Tasks.Task<'a option> when 'a: struct
        
        val insertDeletionItem:
          env: Core.Types.Env -> itemToAdd: Core.Types.RecordedDeletionItem
            -> 'a option when 'a: struct
        
        val internal initializeCacheAsync:
          env: Core.Types.Env -> System.Threading.Tasks.Task<Core.Types.Cache>
        
        val internal initializeCache: env: Core.Types.Env -> Core.Types.Cache
        
        val isApprovedAsync:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer
            -> System.Threading.Tasks.Task<bool>
        
        val isApproved:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer -> bool
        
        val selectDeletionItemsAsync:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer
            -> System.Threading.Tasks.Task<(Core.Types.RecordedDeletionItem *
                                            int64) list>
        
        val selectDeletionItems:
          env: Core.Types.Env -> employer: Core.Types.RecordedEmployer
            -> (Core.Types.RecordedDeletionItem * int64) list
        
        val updateIsApprovedEmployerAsync:
          env: Core.Types.Env -> isApproved: bool
          -> employer: Core.Types.RecordedEmployer
            -> System.Threading.Tasks.Task<Result<Core.Types.RecordedEmployer,
                                                  Core.Types.DatabaseError>>
        
        val updateIsApprovedEmployer:
          env: Core.Types.Env -> isApproved: bool
          -> employer: Core.Types.RecordedEmployer
            -> Result<Core.Types.RecordedEmployer,Core.Types.DatabaseError>
        
        val setIsHiddenTrueForItemAsync:
          env: Core.Types.Env -> itemId: int64
            -> System.Threading.Tasks.Task<Result<int64,Core.Types.DatabaseError>>
        
        val setIsHiddenTrueForItem:
          env: Core.Types.Env -> itemId: int64
            -> Result<int64,Core.Types.DatabaseError>
        
        val setIsDeletionTrueForAllItemsInOfficeAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<Result<int,Core.Types.DatabaseError>>
        
        val setIsDeletionTrueForAllItemsInOffice:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> Result<int,Core.Types.DatabaseError>
        
        val tryDeleteOfficeByOfficeNameAndUpdateCacheAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<Result<Core.Types.RecordedOffice,
                                                  Core.Types.DatabaseError>>
        
        val tryDeleteOfficeByOfficeNameAndUpdateCache:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> Result<Core.Types.RecordedOffice,Core.Types.DatabaseError>
        
        val selectAllItemsByOfficeAsync:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> System.Threading.Tasks.Task<Result<{| Id: int64; IsDeletion: bool;
                                                     IsHidden: bool;
                                                     RecordedItem: Core.Types.RecordedDeletionItem |} list,
                                                  Core.Types.DatabaseError>>
        
        val selectAllItemsByOffice:
          env: Core.Types.Env -> office: Core.Types.RecordedOffice
            -> Result<{| Id: int64; IsDeletion: bool; IsHidden: bool;
                         RecordedItem: Core.Types.RecordedDeletionItem |} list,
                      Core.Types.DatabaseError>

