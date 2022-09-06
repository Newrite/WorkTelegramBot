namespace WorkTelegram.Infrastructure
    
    type ILogger =
        
        abstract Debug: string -> unit
        
        abstract Error: string -> unit
        
        abstract Fatal: string -> unit
        
        abstract Info: string -> unit
        
        abstract Warning: string -> unit
    
    type ILog =
        
        abstract Logger: ILogger
    
    module Logger =
        
        val debug: env: #ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val error: env: #ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val warning: env: #ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val info: env: #ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val fatal: env: #ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val ILogBuilder:
          info: (string -> unit) ->
            warning: (string -> unit) ->
            error: (string -> unit) ->
            fatal: (string -> unit) -> debug: (string -> unit) -> ILog

