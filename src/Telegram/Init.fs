namespace WorkTelegram.Telegram

open WorkTelegram.Core

open FSharp.UMX
open Funogram.Telegram.Types
open WorkTelegram.Infrastructure

module Init =

  let init env (history: System.Collections.Generic.Stack<_>) message =

    let chatId = %message.Chat.Id

    history.Clear()

    let startInit () =

      let manager = Cache.tryGetManagerByChatId env chatId
      let employer = Cache.tryGetEmployerByChatId env chatId

      if manager.IsSome then
        let offices = Cache.getOfficesByManagerId env manager.Value.ChatId

        match offices.Length with
        | 0 ->
          { ManagerProcess.ManagerContext.Manager = manager.Value
            ManagerProcess.ManagerContext.Model = ManagerProcess.Model.NoOffices }
          |> CoreModel.Manager
        | 1 ->
          { ManagerProcess.ManagerContext.Manager = manager.Value
            ManagerProcess.ManagerContext.Model = ManagerProcess.Model.InOffice offices[0] }
          |> CoreModel.Manager
        | _ ->
          { ManagerProcess.ManagerContext.Manager = manager.Value
            ManagerProcess.ManagerContext.Model = ManagerProcess.Model.ChooseOffice offices }
          |> CoreModel.Manager
      elif employer.IsSome then
        { EmployerProcess.EmployerContext.Employer = employer.Value
          EmployerProcess.EmployerContext.Model = EmployerProcess.Model.WaitChoice }
        |> CoreModel.Employer
      else
        AuthProcess.Model.NoAuth |> CoreModel.Auth

    match Database.insertChatId env { ChatId = message.Chat.Id } with
    | Ok _ -> startInit ()
    | Error err -> CoreModel.Error(string err)
