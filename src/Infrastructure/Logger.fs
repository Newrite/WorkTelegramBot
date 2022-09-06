namespace WorkTelegram.Infrastructure

[<Interface>]
type ILogger =
  abstract Error: string -> unit
  abstract Warning: string -> unit
  abstract Fatal: string -> unit
  abstract Debug: string -> unit
  abstract Info: string -> unit

[<Interface>]
type ILog =
  abstract Logger: ILogger

module Logger =

  let debug (env: #ILog) fmt = Printf.kprintf env.Logger.Debug fmt

  let error (env: #ILog) fmt = Printf.kprintf env.Logger.Error fmt

  let warning (env: #ILog) fmt = Printf.kprintf env.Logger.Warning fmt

  let info (env: #ILog) fmt = Printf.kprintf env.Logger.Info fmt

  let fatal (env: #ILog) fmt = Printf.kprintf env.Logger.Fatal fmt

  let ILogBuilder info warning error fatal debug =
    { new ILog with
        member _.Logger =
          { new ILogger with
              member _.Error str = error str
              member _.Info str = info str
              member _.Warning str = warning str
              member _.Fatal str = fatal str
              member _.Debug str = debug str } }
