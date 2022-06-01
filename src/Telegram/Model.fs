namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Core.Types


module AuthProcess =

  [<RequireQualifiedAccess>]
  type Employer =
    | EnteringOffice
    | EnteringLastFirstName of RecordedOffice
    | AskingFinish          of RecordedEmployer

  [<RequireQualifiedAccess>]
  type Manager =
    | EnteringLastFirstName
    | AskingFinish of RecordedManager

  [<RequireQualifiedAccess>]
  type Model =
    | Employer    of Employer
    | Manager     of Manager
    | NoAuth

module EmployerProcess =
  
  [<RequireQualifiedAccess>]
  type Deletion =
    | EnteringName
    | EnteringMac      of ItemWithSerial
    | EnteringSerial   of ItemWithOnlyName
    | EnteringCount    of Item
    | EnteringLocation of Item * PositiveInt
    | AskingFinish     of RecordedDeletionItem

  [<RequireQualifiedAccess>]
  type Model =
    | Deletion   of Deletion
    | WaitChoice
    | EditRecordedDeletions

  type EmployerContext =
    { Employer: RecordedEmployer
      Model:    Model }

  with
  member self.UpdateModel model =
    { self with Model = model }

module ManagerProcess =

  [<RequireQualifiedAccess>]
  type MakeOffice =
    | EnteringName
    | AskingFinish of RecordedOffice

  [<RequireQualifiedAccess>]
  type Model =
    | NoOffices
    | MakeOffice      of MakeOffice
    | ChooseOffice    of RecordedOffice list
    | InOffice        of RecordedOffice
    | AuthEmployers   of RecordedOffice
    | DeAuthEmployers of RecordedOffice

  type ManagerContext =
    { Manager: RecordedManager
      Model:   Model }

  with
  member self.UpdateModel model =
    { self with Model = model }

[<RequireQualifiedAccess>]
type CoreModel =
  | Employer of EmployerProcess.EmployerContext
  | Manager  of ManagerProcess.ManagerContext
  | Auth     of AuthProcess.Model
  | Error    of string