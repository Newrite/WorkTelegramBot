namespace WorkTelegram.Telegram

open WorkTelegram.Core

module AuthProcess =

  [<RequireQualifiedAccess>]
  type Employer =
    | EnteringOffice
    | EnteringLastFirstName of Office
    | AskingFinish of RecordEmployer

  [<RequireQualifiedAccess>]
  type Manager =
    | EnteringLastFirstName
    | AskingFinish of ManagerDto

  [<RequireQualifiedAccess>]
  type Model =
    | Employer of Employer
    | Manager of Manager
    | NoAuth

module EmployerProcess =

  [<RequireQualifiedAccess>]
  type Deletion =
    | EnteringName
    | EnteringMac of ItemWithSerial
    | EnteringSerial of ItemWithOnlyName
    | EnteringCount of Item
    | EnteringLocation of Item * PositiveInt
    | AskingFinish of RecordDeletionItem

  [<RequireQualifiedAccess>]
  type Model =
    | Deletion of Deletion
    | WaitChoice
    | EditDeletionItems

  type EmployerContext =
    { Employer: Employer
      Model: Model }

    member self.UpdateModel model = { self with Model = model }

module ManagerProcess =

  [<RequireQualifiedAccess>]
  type MakeOffice =
    | EnteringName
    | AskingFinish of RecordOffice

  [<RequireQualifiedAccess>]
  type Model =
    | NoOffices
    | MakeOffice of MakeOffice
    | ChooseOffice of Office list
    | InOffice of Office
    | AuthEmployers of Office
    | DeAuthEmployers of Office

  type ManagerContext =
    { Manager: Manager
      Model: Model }

    member self.UpdateModel model = { self with Model = model }

[<AutoOpen>]
module Model =

  open FSharp.UMX
  open Funogram.Telegram.Types
  open WorkTelegram.Infrastructure

  exception private NegativeOfficesCountException of string

  [<RequireQualifiedAccess>]
  type CoreModel =
    | Employer of EmployerProcess.EmployerContext
    | Manager of ManagerProcess.ManagerContext
    | Auth of AuthProcess.Model
    | Error of string

    static member Init env (history: System.Collections.Generic.Stack<_>) message =
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
          | _ when offices.Length > 1 ->
            { ManagerProcess.ManagerContext.Manager = manager.Value
              ManagerProcess.ManagerContext.Model = ManagerProcess.Model.ChooseOffice offices }
            |> CoreModel.Manager
          | _ ->
            NegativeOfficesCountException($"Offices count is {offices.Length}")
            |> raise
        elif employer.IsSome then
          { EmployerProcess.EmployerContext.Employer = employer.Value
            EmployerProcess.EmployerContext.Model = EmployerProcess.Model.WaitChoice }
          |> CoreModel.Employer
        else
          AuthProcess.Model.NoAuth |> CoreModel.Auth

      match Database.insertChatId env { ChatId = message.Chat.Id } with
      | Result.Ok _ -> startInit ()
      | Result.Error err -> CoreModel.Error(string err)
