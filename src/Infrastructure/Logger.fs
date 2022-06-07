namespace WorkTelegram.Infrastructure

module Logger =

  let debug (env: #ILog) fmt = Printf.kprintf env.Logger.Debug fmt

  let error (env: #ILog) fmt = Printf.kprintf env.Logger.Error fmt

  let warning (env: #ILog) fmt = Printf.kprintf env.Logger.Warning fmt

  let info (env: #ILog) fmt = Printf.kprintf env.Logger.Info fmt

  let fatal (env: #ILog) fmt = Printf.kprintf env.Logger.Fatal fmt

  let ILogBuilder info warning error fatal debug =
    { new ILog with
        member _.Logger =
          { new IAppLogger with
              member _.Error str = error str
              member _.Info str = info str
              member _.Warning str = warning str
              member _.Fatal str = fatal str
              member _.Debug str = debug str } }
