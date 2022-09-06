namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Infrastructure

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
    | ChooseOffice of OfficesMap
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

    static member Init env (message: TelegramMessage) =
      let chatId = %message.Chat.Id

      let history = System.Collections.Generic.Stack<CoreModel>()

      let startInit () =

        let manager = Repository.tryManagerByChatId env chatId
        let employer = Repository.tryEmployerByChatId env chatId

        if manager.IsSome then
          let offices = Repository.tryOfficeByChatId env manager.Value.ChatId

          match offices.Count with
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
                  ManagerProcess.ManagerModel.InOffice (offices |> Map.toArray |> Array.head |> snd)}

            { History = history
              Model = CoreModel.Manager ctx }
          | _ when offices.Count > 1 ->
            let ctx =
              { ManagerProcess.ManagerContext.Manager = manager.Value
                ManagerProcess.ManagerContext.Model =
                  ManagerProcess.ManagerModel.ChooseOffice offices }

            { History = history
              Model = CoreModel.Manager ctx }
          | _ ->
            NegativeOfficesCountException($"Offices count is {offices.Count}")
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

      match Repository.tryAddChatId env %message.Chat.Id with
      | true -> startInit ()
      | false ->
        { History = history
          Model = CoreModel.Error("Произошла ошибка при инициализации, попробуйте еще раз позже.") }
