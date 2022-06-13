namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Telegram

module AuthProcess =

  [<RequireQualifiedAccess>]
  type AuthEmployer =
    | EnteringOffice
    | EnteringLastFirstName of Office
    | AskingFinish of Employer

  [<RequireQualifiedAccess>]
  type AuthManager =
    | EnteringLastFirstName
    | AskingFinish of Manager

  [<RequireQualifiedAccess>]
  type AuthModel =
    | Employer of AuthEmployer
    | Manager of AuthManager
    | NoAuth

module EmployerProcess =

  [<RequireQualifiedAccess>]
  type Deletion =
    | EnteringName
    | EnteringMac of ItemWithSerial
    | EnteringSerial of ItemWithOnlyName
    | EnteringCount of Item
    | EnteringLocation of Item * PositiveInt
    | AskingFinish of DeletionItem

  [<RequireQualifiedAccess>]
  type EmployerModel =
    | Deletion of Deletion
    | WaitChoice
    | EditDeletionItems

  type EmployerContext =
    { Employer: Employer
      Model: EmployerModel }

    member self.UpdateModel model = { self with Model = model }

module ManagerProcess =

  [<RequireQualifiedAccess>]
  type MakeOffice =
    | EnteringName
    | AskingFinish of Office

  [<RequireQualifiedAccess>]
  type ManagerModel =
    | NoOffices
    | MakeOffice of MakeOffice
    | ChooseOffice of Office list
    | InOffice of Office
    | AuthEmployers of Office
    | DeAuthEmployers of Office

  type ManagerContext =
    { Manager: Manager
      Model: ManagerModel }

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
    | Auth of AuthProcess.AuthModel
    | Error of string

  [<NoComparison>]
  type ModelContext<'Model> =
    { History: System.Collections.Generic.Stack<'Model>
      Model: CoreModel }

    member self.Transform model = { self with Model = model }

    static member Init env message =
      let chatId = %message.Chat.Id

      let history = System.Collections.Generic.Stack<CoreModel>()

      let startInit () =

        let manager = Cache.tryGetManagerByChatId env chatId
        let employer = Cache.tryGetEmployerByChatId env chatId

        if manager.IsSome then
          let offices = Cache.getOfficesByManagerId env manager.Value.ChatId

          match offices.Length with
          | 0 ->
            let ctx =
              { ManagerProcess.ManagerContext.Manager = manager.Value
                ManagerProcess.ManagerContext.Model = ManagerProcess.ManagerModel.NoOffices }

            { History = history
              Model = CoreModel.Manager ctx }
          | 1 ->
            let ctx =
              { ManagerProcess.ManagerContext.Manager = manager.Value
                ManagerProcess.ManagerContext.Model =
                  ManagerProcess.ManagerModel.InOffice offices[0] }

            { History = history
              Model = CoreModel.Manager ctx }
          | _ when offices.Length > 1 ->
            let ctx =
              { ManagerProcess.ManagerContext.Manager = manager.Value
                ManagerProcess.ManagerContext.Model =
                  ManagerProcess.ManagerModel.ChooseOffice offices }

            { History = history
              Model = CoreModel.Manager ctx }
          | _ ->
            NegativeOfficesCountException($"Offices count is {offices.Length}")
            |> raise
        elif employer.IsSome then
          let ctx =
            { EmployerProcess.EmployerContext.Employer = employer.Value
              EmployerProcess.EmployerContext.Model = EmployerProcess.EmployerModel.WaitChoice }

          { History = history
            Model = CoreModel.Employer ctx }
        else
          { History = history
            Model = CoreModel.Auth AuthProcess.AuthModel.NoAuth }

      match Database.insertChatId env { ChatId = message.Chat.Id } with
      | Result.Ok _ -> startInit ()
      | Result.Error err ->
        { History = history
          Model = CoreModel.Error(string err) }
