namespace WorkTelegram.Infrastructure

open WorkTelegram.Infrastructure

module Configurer =

  let botConfig (env: #ICfg<_>) = env.Configurer.BotConfig

  let elmishDict (env: #ICfg<_>) = env.Configurer.ElmishDict

  let IConfigurerBuilder config dict =
    { new ICfg<_> with
        member _.Configurer =
          { new IConfigurer<_> with
              member _.BotConfig = config
              member _.ElmishDict = dict } }
