﻿namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Core.Types


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

[<RequireQualifiedAccess>]
type CoreModel =
  | Employer of EmployerProcess.EmployerContext
  | Manager of ManagerProcess.ManagerContext
  | Auth of AuthProcess.Model
  | Error of string
