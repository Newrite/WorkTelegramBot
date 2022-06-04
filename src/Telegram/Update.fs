namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Infrastructure

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type UpdateMessage =
  | AuthManagerChange of AuthProcess.Manager
  | FinishEmployerAuth of Employer * Env
  | FinishManagerAuth of Manager * Env
  | ManagerChooseOffice of ManagerProcess.ManagerContext * Office
  | ManagerMakeOfficeChange of ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
  | FinishMakeOfficeProcess of Office * Env
  | StartEditRecordedItems of EmployerProcess.EmployerContext
  | StartAuthEmployers of ManagerProcess.ManagerContext * Office
  | StartDeAuthEmployers of ManagerProcess.ManagerContext * Office
  | DeletionProcessChange of EmployerProcess.EmployerContext * EmployerProcess.Deletion
  | AuthEmployerChange of AuthProcess.Employer
  | FinishDeletionProcess of EmployerProcess.EmployerContext * DeletionItem * Env
  | Back
  | Cancel
  | NothingChange

module Update =

  let update
    (history: System.Collections.Generic.Stack<_>)
    message
    model
    callInitilizationModelFunction
    =
    match message with
    | UpdateMessage.Back
    | UpdateMessage.Cancel
    | UpdateMessage.NothingChange -> ()
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
    | UpdateMessage.StartEditRecordedItems state ->
      { state with Model = EmployerProcess.Model.EditRecordedDeletions }
      |> CoreModel.Employer
    | UpdateMessage.StartAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.Model.AuthEmployers office }
      |> CoreModel.Manager
    | UpdateMessage.StartDeAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.Model.DeAuthEmployers office }
      |> CoreModel.Manager
    | UpdateMessage.FinishDeletionProcess (state, rDeletionItem, env) ->
      match Database.insertDeletionItem env rDeletionItem with
      | Some i ->
        if i < 0 then
          let text =
            "Добавление позиции в базу данных не удалось, 
              предположительно такой мак или серийный номер уже списаны"

          Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 3000
        else
          let text = $"Позиция успешно добавлена в базу данных"
          Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 5000
      | None ->
        let text =
          "Добавление позиции в базу данных не удалось, 
              предположительно база данных недоступна, попробуйте позже"

        Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 3000

      callInitilizationModelFunction ()
    | UpdateMessage.ManagerChooseOffice (state, office) ->
      { state with Model = ManagerProcess.Model.InOffice office }
      |> CoreModel.Manager
    | UpdateMessage.ManagerMakeOfficeChange (state, newChange) ->
      { state with Model = ManagerProcess.Model.MakeOffice newChange }
      |> CoreModel.Manager
    | UpdateMessage.FinishManagerAuth (manager, env) ->
      match Database.insertManager env manager with
      | Ok _ ->
        let text = $"Позиция успешно добавлена в базу данных"
        Utils.sendMessageAndDeleteAfterDelay env manager.ChatId text 5000
      | Error err ->
        let text = $"Добавление позиции в базу данных не удалось: {err}"
        Utils.sendMessageAndDeleteAfterDelay env manager.ChatId text 3000

      callInitilizationModelFunction ()
    | UpdateMessage.FinishMakeOfficeProcess (office, env) ->
      match Database.insertOffice env office with
      | Ok _ ->
        let text = $"Позиция успешно добавлена в базу данных"
        Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 5000
      | Error err ->
        let text = $"Добавление позиции в базу данных не удалось: {err}"
        Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 3000

      callInitilizationModelFunction ()
    | UpdateMessage.Cancel -> callInitilizationModelFunction ()
    | UpdateMessage.NothingChange -> model
    | UpdateMessage.Back ->
      if history.Count > 0 then
        history.Pop()
      else
        model
    | UpdateMessage.FinishEmployerAuth (employer, env) ->
      match Database.insertEmployer env employer false with
      | Ok _ ->
        let text = $"Позиция успешно добавлена в базу данных"
        Utils.sendMessageAndDeleteAfterDelay env employer.ChatId text 5000
      | Error err ->
        let text = $"Добавление позиции в базу данных не удалось: {err}"
        Utils.sendMessageAndDeleteAfterDelay env employer.ChatId text 3000

      callInitilizationModelFunction ()
