namespace WorkTelegram.Infrastructure
    
    module Logger =
        
        val debug: env: #AppEnv.ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val error: env: #AppEnv.ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val warning:
          env: #AppEnv.ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val info: env: #AppEnv.ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val fatal: env: #AppEnv.ILog -> fmt: Printf.StringFormat<'b,unit> -> 'b
        
        val ILogBuilder:
          info: (string -> unit) ->
            warning: (string -> unit) ->
            error: (string -> unit) ->
            fatal: (string -> unit) -> debug: (string -> unit) -> AppEnv.ILog

