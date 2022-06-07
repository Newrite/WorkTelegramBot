namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open FSharp.UMX

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type UpdateMessage =
  | AuthManagerChange of AuthProcess.Manager
  | FinishEmployerAuth of RecordEmployer
  | FinishManagerAuth of ManagerDto
  | ManagerChooseOffice of ManagerProcess.ManagerContext * Office
  | ManagerMakeOfficeChange of ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
  | FinishMakeOfficeProcess of RecordOffice
  | StartEditDeletionItems of EmployerProcess.EmployerContext
  | StartAuthEmployers of ManagerProcess.ManagerContext * Office
  | StartDeAuthEmployers of ManagerProcess.ManagerContext * Office
  | DeletionProcessChange of EmployerProcess.EmployerContext * EmployerProcess.Deletion
  | AuthEmployerChange of AuthProcess.Employer
  | FinishDeletionProcess of EmployerProcess.EmployerContext * RecordDeletionItem
  | Back
  | Cancel
  | ReRender

module Update =

  let update
    env
    (history: System.Collections.Generic.Stack<_>)
    message
    model
    callInitModelFunction
    =
    match message with
    | UpdateMessage.Back
    | UpdateMessage.Cancel
    | UpdateMessage.ReRender -> ()
    | _ -> history.Push(model)

    match message with
    | UpdateMessage.AuthManagerChange newAuth ->
      newAuth
      |> AuthProcess.Model.Manager
      |> CoreModel.Auth
    | UpdateMessage.AuthEmployerChange newAuth ->
      newAuth
      |> AuthProcess.Model.Employer
      |> CoreModel.Auth
    | UpdateMessage.DeletionProcessChange (state, newProces) ->
      EmployerProcess.Model.Deletion newProces
      |> state.UpdateModel
      |> CoreModel.Employer
    | UpdateMessage.StartEditDeletionItems state ->
      { state with Model = EmployerProcess.Model.EditDeletionItems }
      |> CoreModel.Employer
    | UpdateMessage.StartAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.Model.AuthEmployers office }
      |> CoreModel.Manager
    | UpdateMessage.StartDeAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.Model.DeAuthEmployers office }
      |> CoreModel.Manager
    | UpdateMessage.FinishDeletionProcess (state, rDeletionItem) ->
      match Cache.tryAddDeletionItemInDb env rDeletionItem with
      | Some _ ->
        let text = "Позиция успешно добавлена в базу данных"
        Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 5000
      | None ->
        let text =
          "Добавление позиции в базу данных не удалось, 
              предположительно база данных недоступна, попробуйте позже"

        Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.ManagerChooseOffice (state, office) ->
      { state with Model = ManagerProcess.Model.InOffice office }
      |> CoreModel.Manager
    | UpdateMessage.ManagerMakeOfficeChange (state, newChange) ->
      { state with Model = ManagerProcess.Model.MakeOffice newChange }
      |> CoreModel.Manager
    | UpdateMessage.FinishManagerAuth manager ->
      match Cache.tryAddManagerInDb env manager with
      | Some _ ->
        let text = "Аутентификация прошла успешно"
        Utils.sendMessageAndDeleteAfterDelay env %manager.ChatId text 5000
      | None ->
        let text = "Провести аутентификацию не удалось, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %manager.ChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.FinishMakeOfficeProcess office ->
      match Cache.tryAddOfficeInDb env office with
      | Some _ ->
        let text = "Офис успешно создан"
        Utils.sendMessageAndDeleteAfterDelay env %office.ManagerChatId text 5000
      | None ->
        let text = "Произошла ошибка при создании офиса, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %office.ManagerChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.Cancel -> callInitModelFunction ()
    | UpdateMessage.ReRender -> model
    | UpdateMessage.Back ->
      if history.Count > 0 then
        history.Pop()
      else
        model
    | UpdateMessage.FinishEmployerAuth employer ->
      match Cache.tryAddEmployerInDb env employer with
      | Some _ ->
        let text = "Аутентификация прошла успешно"
        Utils.sendMessageAndDeleteAfterDelay env %employer.ChatId text 5000
      | None ->
        let text = "Провести аутентификацию не удалось, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %employer.ChatId text 3000

      callInitModelFunction ()
