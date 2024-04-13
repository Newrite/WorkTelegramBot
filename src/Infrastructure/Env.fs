namespace WorkTelegram.Infrastructure

type IAppEnv<'ElmishCommand, 'CacheCommand> =
  inherit ILog
  inherit IDb
  inherit IRep<'CacheCommand>
  inherit ICfg<'ElmishCommand>
  inherit IEventBus

[<AutoOpen>]
module AppEnv =

  let IAppEnvBuilder iLog iDb iRep iCfg iBus =
    { new IAppEnv<_, _> with
        member _.Logger = iLog
        member _.Db = iDb
        member _.Repository = iRep
        member _.Configurer = iCfg
        member _.Bus = iBus }
