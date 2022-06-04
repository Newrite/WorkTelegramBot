namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

open System

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type CacheCommand =
  | Initialization of Env
  | EmployerByChatId of ChatId * AsyncReplyChannel<Employer option>
  | ManagerByChatId of ChatId * AsyncReplyChannel<Manager option>
  //| OfficeByManagerChatId of ChatId * AsyncReplyChannel<RecordedOffice   option>
  | Offices of AsyncReplyChannel<Office list option>
  | AddOffice of Office
  | AddEmployer of Employer
  | AddManager of Manager
  | CurrentCache of AsyncReplyChannel<Cache>
  | GetOfficeEmployers of Office * AsyncReplyChannel<Employer list option>
  | DeleteOffice of Office

module Cache =

  let cacheActor (inbox: MailboxProcessor<CacheCommand>) =

    let rec waitInit () =
      async {
        let! msg = inbox.Receive()

        match msg with
        | CacheCommand.Initialization env ->
          let cache = Database.initializeCache env
          env.Log.Info "Cache success initialization and start"
          return! cacheHandler env cache
        | _ -> return! waitInit ()
      }

    and cacheHandler (env: Env) (cache: Cache) =
      async {
        let! msg = inbox.Receive()

        match msg with
        | CacheCommand.EmployerByChatId (chatId, channel) ->
          let employer =
            cache.Employers
            |> List.tryFind ^ fun e -> e.ChatId = chatId

          match employer with
          | Some _ ->
            channel.Reply employer
            return! cacheHandler env cache
          | None ->
            match Database.selectEmployerByChatId env chatId with
            | Some e ->
              channel.Reply ^ Some e
              return! cacheHandler env { cache with Employers = e :: cache.Employers }
            | None ->
              channel.Reply None
              return! cacheHandler env cache
        | CacheCommand.ManagerByChatId (chatId, channel) ->
          let manager =
            cache.Managers
            |> List.tryFind ^ fun m -> m.ChatId = chatId

          match manager with
          | Some _ ->
            channel.Reply manager
            return! cacheHandler env cache
          | None ->
            match Database.selectManagerByChatId env chatId with
            | Some m ->
              channel.Reply ^ Some m
              return! cacheHandler env { cache with Managers = m :: cache.Managers }
            | None ->
              channel.Reply None
              return! cacheHandler env cache
        //| CacheCommand.OfficeByManagerChatId (chatId, channel) ->
        //  let office =
        //    cache.Offices
        //    |> List.tryFind ^ fun o -> o.Manager.ChatId = chatId
        //  match office with
        //  | Some _ ->
        //    channel.Reply office
        //    return! cacheHandler env cache
        //  | None ->
        //    match Database.selectOfficeByManagerChatId env chatId with
        //    | Some o ->
        //      channel.Reply ^ Some o
        //      return! cacheHandler env { cache with Offices = o :: cache.Offices }
        //    | None ->
        //      channel.Reply None
        //      return! cacheHandler env cache
        | CacheCommand.Offices channel ->
          cache.Offices |> Some |> channel.Reply
          return! cacheHandler env cache
        | CacheCommand.GetOfficeEmployers (office, channel) ->
          cache.Employers
          |> List.filter (fun e -> e.Office = office)
          |> fun el -> if el.IsEmpty then None else Some el
          |> channel.Reply

          return! cacheHandler env cache
        | CacheCommand.DeleteOffice office ->
          return! cacheHandler env { cache with Offices = List.except [ office ] cache.Offices }
        | CacheCommand.AddOffice office ->
          return! cacheHandler env { cache with Offices = office :: cache.Offices }
        | CacheCommand.AddEmployer employer ->
          return! cacheHandler env { cache with Employers = employer :: cache.Employers }
        | CacheCommand.AddManager manager ->
          return! cacheHandler env { cache with Managers = manager :: cache.Managers }
        | CacheCommand.CurrentCache channel ->
          channel.Reply cache
          return! cacheHandler env cache
        | CacheCommand.Initialization _ -> return! cacheHandler env cache
      }

    waitInit ()

  let private reply env asyncReplyChannel =
    try
      env.CacheActor.PostAndReply(asyncReplyChannel, 30000)
    with
    | :? TimeoutException as _ ->
      env.Log.Error "Timeout exception when try get data from cache actor, return None"
      None

  let employerByChatId env chatId =
    env.Log.Debug $"Get employer by chat id with id = {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.EmployerByChatId(chatId, channel)

  let employerByChatIdAsync env chatId = task { return employerByChatId env chatId }

  let managerByChatId env chatId =
    env.Log.Debug $"Get manager by chat id with id = {chatId} from cache"

    reply env
    ^ fun channel -> CacheCommand.ManagerByChatId(chatId, channel)

  let managerByChatIdAsync env chatId = task { return managerByChatId env chatId }

  //let officeByManagerChatId env chatId =
  //  env.Log.Debug $"Get office by manager id with id = {chatId} from cache"
  //  reply env ^ fun channel ->
  //    CacheCommand.OfficeByManagerChatId(chatId, channel)
  //
  //let officeByManagerChatIdAsync env chatId =
  //  task {
  //    return officeByManagerChatId env chatId
  //  }

  let offices env =
    env.Log.Debug $"Get offices from cache"

    reply env
    ^ fun channel -> CacheCommand.Offices(channel)

  let officesAsync env = task { return offices env }

  let officeEmployers env office =
    env.Log.Debug $"Get office employers from cache"

    reply env
    ^ fun channel -> CacheCommand.GetOfficeEmployers(office, channel)

  let officeEmployersAsync env office = task { return officeEmployers env office }

  let addOffice env office =
    env.Log.Debug $"Add new office to cache"
    env.CacheActor.Post(CacheCommand.AddOffice office)

  let addOfficeAsync env office = task { return addOffice env office }

  let addEmployer env employer =
    env.Log.Debug $"Add new employer to cache"
    env.CacheActor.Post(CacheCommand.AddEmployer employer)

  let addEmployerAsync env employer = task { return addEmployer env employer }

  let addManager env manager =
    env.Log.Debug $"Add new manager to cache"
    env.CacheActor.Post(CacheCommand.AddManager manager)

  let addManagerAsync env manager = task { return addManager env manager }

  let initialization env =
    env.Log.Info "Start init cache"
    env.CacheActor.Post(CacheCommand.Initialization env)
