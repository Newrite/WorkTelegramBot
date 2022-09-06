namespace WorkTelegram.Infrastructure

open System.Collections.Concurrent

[<Interface>]
type IConfigurer<'ElmishCommand> =
  abstract BotConfig: Funogram.Types.BotConfig
  abstract ElmishDict: ConcurrentDictionary<int64, Agent<'ElmishCommand>>

[<Interface>]
type ICfg<'ElmishCommand> =
  abstract Configurer: IConfigurer<'ElmishCommand>

module Configurer =

  let botConfig (env: #ICfg<_>) = env.Configurer.BotConfig

  let elmishDict (env: #ICfg<_>) = env.Configurer.ElmishDict

  let IConfigurerBuilder config dict =
    { new ICfg<_> with
        member _.Configurer =
          { new IConfigurer<_> with
              member _.BotConfig = config
              member _.ElmishDict = dict } }
