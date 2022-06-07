namespace WorkTelegram.Infrastructure

open WorkTelegram.Infrastructure

module Configurer =
  
  let botConfig (env: #ICfg) =
    env.Configurer.BotConfig
    
  let IConfigurerBuilder config =
    { new ICfg with
        member _.Configurer =
          { new IConfigurer with
              member _.BotConfig = config } }