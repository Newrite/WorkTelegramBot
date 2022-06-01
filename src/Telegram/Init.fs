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

      let manager = Cache.managerByChatId env chatId
      let employer = Cache.employerByChatId env chatId

      if manager.IsSome then
        let offices = Database.selectOfficesByManagerChatId env manager.Value.ChatId

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

    match Database.insertChatId env chatId with
    | Ok _ -> startInit ()
    | Error err ->

    match err with
    | DatabaseError.ChatIdAlreadyExistInDatabase _ -> startInit ()
    | DatabaseError.SQLiteException exn -> CoreModel.Error exn.Message
    | _ -> startInit ()
