namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open FSharp.UMX

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type UpdateMessage =
  | AuthManagerChange of AuthProcess.AuthManager
  | FinishEmployerAuth of Employer
  | FinishManagerAuth of Manager
  | ManagerChooseOffice of ManagerProcess.ManagerContext * Office
  | ManagerMakeOfficeChange of ManagerProcess.ManagerContext * ManagerProcess.MakeOffice
  | FinishMakeOfficeProcess of Office
  | StartEditDeletionItems of EmployerProcess.EmployerContext
  | StartAuthEmployers of ManagerProcess.ManagerContext * Office
  | StartDeAuthEmployers of ManagerProcess.ManagerContext * Office
  | DeletionProcessChange of EmployerProcess.EmployerContext * EmployerProcess.Deletion
  | AuthEmployerChange of AuthProcess.AuthEmployer
  | FinishDeletionProcess of EmployerProcess.EmployerContext * DeletionItem
  | Back
  | Cancel
  | ReRender

module Update =

  let update env message model callInitModelFunction =
    match message with
    | UpdateMessage.Back
    | UpdateMessage.Cancel
    | UpdateMessage.ReRender -> ()
    | _ -> model.History.Push(model.Model)

    match message with
    | UpdateMessage.AuthManagerChange newAuth ->
      newAuth
      |> AuthProcess.AuthModel.Manager
      |> CoreModel.Auth
      |> model.Transform
    | UpdateMessage.AuthEmployerChange newAuth ->
      newAuth
      |> AuthProcess.AuthModel.Employer
      |> CoreModel.Auth
      |> model.Transform
    | UpdateMessage.DeletionProcessChange (state, newProces) ->
      EmployerProcess.EmployerModel.Deletion newProces
      |> state.UpdateModel
      |> CoreModel.Employer
      |> model.Transform
    | UpdateMessage.StartEditDeletionItems state ->
      { state with Model = EmployerProcess.EmployerModel.EditDeletionItems }
      |> CoreModel.Employer
      |> model.Transform
    | UpdateMessage.StartAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.ManagerModel.AuthEmployers office }
      |> CoreModel.Manager
      |> model.Transform
    | UpdateMessage.StartDeAuthEmployers (state, office) ->
      { state with Model = ManagerProcess.ManagerModel.DeAuthEmployers office }
      |> CoreModel.Manager
      |> model.Transform
    | UpdateMessage.FinishDeletionProcess (state, rDeletionItem) ->
      match Repository.tryAddDeletionItem env rDeletionItem with
      | true ->
        let text = "Позиция успешно добавлена в базу данных"
        Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 5000
      | false ->
        let text =
          "Добавление позиции в базу данных не удалось, 
              предположительно база данных недоступна, попробуйте позже"

        Utils.sendMessageAndDeleteAfterDelay env state.Employer.ChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.ManagerChooseOffice (state, office) ->
      { state with Model = ManagerProcess.ManagerModel.InOffice office }
      |> CoreModel.Manager
      |> model.Transform
    | UpdateMessage.ManagerMakeOfficeChange (state, newChange) ->
      { state with Model = ManagerProcess.ManagerModel.MakeOffice newChange }
      |> CoreModel.Manager
      |> model.Transform
    | UpdateMessage.FinishManagerAuth manager ->
      match Repository.tryAddManager env manager with
      | true ->
        let text = "Аутентификация прошла успешно"
        Utils.sendMessageAndDeleteAfterDelay env %manager.ChatId text 5000
      | false ->
        let text = "Провести аутентификацию не удалось, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %manager.ChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.FinishMakeOfficeProcess office ->
      match Repository.tryAddOffice env office with
      | true ->
        let text = "Офис успешно создан"
        Utils.sendMessageAndDeleteAfterDelay env %office.Manager.ChatId text 5000
      | false ->
        let text = "Произошла ошибка при создании офиса, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %office.Manager.ChatId text 3000

      callInitModelFunction ()
    | UpdateMessage.Cancel -> callInitModelFunction ()
    | UpdateMessage.ReRender -> model
    | UpdateMessage.Back ->
      if model.History.Count > 0 then
        model.History.Pop() |> model.Transform
      else
        model
    | UpdateMessage.FinishEmployerAuth employer ->
      match Repository.tryAddEmployer env employer with
      | true ->
        let text = "Аутентификация прошла успешно"
        Utils.sendMessageAndDeleteAfterDelay env %employer.ChatId text 5000
      | false ->
        let text = "Провести аутентификацию не удалось, попробуйте попозже"
        Utils.sendMessageAndDeleteAfterDelay env %employer.ChatId text 3000

      callInitModelFunction ()
