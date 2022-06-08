namespace WorkTelegram.Telegram

open FSharp.UMX

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open Elmish
open WorkTelegram.Telegram

module View =

  exception private ViewUnmatchedException of string

  open EmployerProcess
  open ManagerProcess
  open AuthProcess

  [<NoEquality>]
  [<NoComparison>]
  type private ViewContext<'Command> =
    { Dispatch: UpdateMessage -> unit
      BackCancelKeyboard: Keyboard
      AppEnv: IAppEnv<'Command> }


  let private employerProcess (ctx: ViewContext<_>) (employerState: EmployerContext) =

    let waitChoice () =

      if Cache.isApprovedEmployer ctx.AppEnv employerState.Employer then

        let text =
          sprintf
            "Привет %s %s из %s"
            %employerState.Employer.FirstName
            %employerState.Employer.LastName
            %employerState.Employer.Office.OfficeName

        RenderView.create
          text
          [ Keyboard.create [ Button.create "Добавить запись" (fun _ ->
                                UpdateMessage.DeletionProcessChange(
                                  employerState,
                                  Deletion.EnteringName
                                )
                                |> ctx.Dispatch) ]
            Keyboard.create [ Button.create "Удалить запись" (fun _ ->
                                employerState
                                |> UpdateMessage.StartEditDeletionItems
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []
      else

      let text =
        "Ваша учетная запись еще не авторизована менеджером офис которого вы выбрали"

      RenderView.create
        text
        [ Keyboard.create [ Button.create "Обновить" (fun _ -> ctx.Dispatch UpdateMessage.Cancel) ] ]
        []

    let deletionProcess (dprocess: Deletion) =

      match dprocess with
      | Deletion.EnteringName ->

        RenderView.create
          "Введите имя записи"
          [ ctx.BackCancelKeyboard ]
          [ fun message ->
              let name: ItemName = % message.Text.Value.ToUpper()

              UpdateMessage.DeletionProcessChange(
                employerState,
                ItemWithOnlyName.create name
                |> Deletion.EnteringSerial
              )
              |> ctx.Dispatch ]

      | Deletion.EnteringSerial item ->

        RenderView.create
          "Введите серийный номер"
          [ Keyboard.create [ Button.create "Без серийного номера" (fun _ ->
                                UpdateMessage.DeletionProcessChange(
                                  employerState,
                                  item
                                  |> Item.ItemWithOnlyName
                                  |> Deletion.EnteringCount
                                )
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          [ fun message ->
              let serial: Serial = % message.Text.Value.ToUpper()

              UpdateMessage.DeletionProcessChange(
                employerState,
                ItemWithSerial.create item.Name serial
                |> Deletion.EnteringMac
              )
              |> ctx.Dispatch ]

      | Deletion.EnteringMac item ->

        RenderView.create
          "Введите мак адрес"
          [ Keyboard.create [ Button.create "Пропустить" (fun _ ->
                                UpdateMessage.DeletionProcessChange(
                                  employerState,
                                  (item |> Item.ItemWithSerial, PositiveInt.one)
                                  |> Deletion.EnteringLocation
                                )
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          [ fun message ->
              match MacAddress.fromString (message.Text.Value.ToUpper()) with
              | Ok macaddress ->
                let itemWithMacaddress = Item.createWithMacAddress item.Name item.Serial macaddress

                UpdateMessage.DeletionProcessChange(
                  employerState,
                  Deletion.EnteringLocation(itemWithMacaddress, PositiveInt.one)
                )
                |> ctx.Dispatch
              | Error _ ->
                let text = "Некорректный ввод мак адреса, попробуйте еще раз"
                Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000 ]

      | Deletion.EnteringCount item ->

        RenderView.create
          "Введите количество"
          [ ctx.BackCancelKeyboard ]
          [ fun message ->
              match PositiveInt.tryParse message.Text.Value with
              | Ok pint ->
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  Deletion.EnteringLocation(item, pint)
                )
                |> ctx.Dispatch
              | Error err ->
                match err with
                | BusinessError.NumberMustBePositive incorrectNumber ->
                  Utils.sendMessageAndDeleteAfterDelay
                    ctx.AppEnv
                    %message.Chat.Id
                    $"Значение должно быть больше нуля, попробуйте еще раз: Введенное значение {incorrectNumber}"
                    3000
                | BusinessError.IncorrectParsePositiveNumber incorrectString ->
                  Utils.sendMessageAndDeleteAfterDelay
                    ctx.AppEnv
                    %message.Chat.Id
                    $"Некорректный ввод, попробуйте еще раз: Введенное значение {incorrectString}"
                    3000
                | _ ->
                  ViewUnmatchedException($"Unmatched error in view function: error {err}")
                  |> raise ]

      | Deletion.EnteringLocation (item, count) ->

        RenderView.create
          "Введите для чего либо куда"
          [ Keyboard.create [ Button.create "Пропустить" (fun _ ->
                                let recordedDeletion =
                                  Record.createDeletionItem
                                    item
                                    count
                                    System.DateTime.Now
                                    None
                                    employerState.Employer.Office.OfficeId
                                    employerState.Employer.ChatId

                                UpdateMessage.DeletionProcessChange(
                                  employerState,
                                  Deletion.AskingFinish(recordedDeletion)
                                )
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          [ fun message ->
              let location: Location option = %message.Text.Value |> Some

              let recordedDeletion =
                Record.createDeletionItem
                  item
                  count
                  System.DateTime.Now
                  location
                  employerState.Employer.Office.OfficeId
                  employerState.Employer.ChatId

              UpdateMessage.DeletionProcessChange(
                employerState,
                Deletion.AskingFinish(recordedDeletion)
              )
              |> ctx.Dispatch ]
      | Deletion.AskingFinish recordedDeletionItem ->

      let messageText =
        $"Внести эту запись?
           {recordedDeletionItem.ToString()}"

      RenderView.create
        messageText
        [ Keyboard.create [ Button.create "Внести" (fun _ ->
                              UpdateMessage.FinishDeletionProcess(
                                employerState,
                                recordedDeletionItem
                              )
                              |> ctx.Dispatch) ]
          ctx.BackCancelKeyboard ]
        []

    match employerState.Model with
    | Model.WaitChoice -> waitChoice ()

    | Model.Deletion dprocess -> deletionProcess dprocess
    | Model.EditDeletionItems ->

    let items =
      query {
        for item in Cache.getDeletionItems ctx.AppEnv do
          let notInspired =
            let since = (System.DateTime.Now - item.Time)
            since.TotalHours < 24.

          where (
            item.Employer.ChatId = employerState.Employer.ChatId
            && notInspired
          )

          select item
      }
      |> List.ofSeq

    if items.Length < 1 then
      RenderView.create
        "Не нашлось записей внесенных за последние 24 часа"
        [ ctx.BackCancelKeyboard ]
        []
    else
      RenderView.create
        "Выберите запись для удаления"
        [ for item in items do
            Keyboard.create [ let text =
                                let serial =
                                  match item.Item.Serial with
                                  | Some serial -> %serial
                                  | None -> "_"

                                let mac =
                                  match item.Item.MacAddress with
                                  | Some macaddress -> macaddress.GetValue
                                  | None -> "_"

                                $"{item.Item.Name} - Serial {serial} - Mac {mac}"

                              Button.create text (fun _ ->
                                match Cache.tryHideDeletionItem ctx.AppEnv item.DeletionId with
                                | true ->
                                  let text = "Запись успешно удалена"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    ctx.AppEnv
                                    employerState.Employer.ChatId
                                    text
                                    3000

                                  ctx.Dispatch UpdateMessage.ReRender
                                | false ->
                                  let text =
                                    "Не удалось удалить запись, попробуйте еще раз или попозже"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    ctx.AppEnv
                                    employerState.Employer.ChatId
                                    text
                                    3000

                                  ctx.Dispatch UpdateMessage.ReRender) ]
          ctx.BackCancelKeyboard ]
        []


  let private authProcess (ctx: ViewContext<_>) authModel =

    let authEmployer eauth =

      let renderOffices (offices: Office list) =

        RenderView.create
          "Выберите офис из списка"
          [ for office in offices do
              Keyboard.create [ Button.create %office.OfficeName (fun _ ->
                                  office
                                  |> Employer.EnteringLastFirstName
                                  |> UpdateMessage.AuthEmployerChange
                                  |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []

      match eauth with

      | Employer.EnteringOffice ->
        let offices = Cache.getOffices ctx.AppEnv

        if offices.Length > 0 then
          renderOffices offices
        else
          RenderView.create "Нет офисов" [ ctx.BackCancelKeyboard ] []

      | Employer.EnteringLastFirstName office ->
        RenderView.create
          "Введите имя и фамилию"
          [ ctx.BackCancelKeyboard ]
          [ fun message ->
              let array = message.Text.Value.Split(' ')

              if array.Length = 2 then
                let firstName, lastName: FirstName * LastName = %array[0], %array[1]

                let employer =
                  Record.createEmployer
                    office.OfficeId
                    office.OfficeName
                    firstName
                    lastName
                    %message.Chat.Id

                employer
                |> Employer.AskingFinish
                |> UpdateMessage.AuthEmployerChange
                |> ctx.Dispatch
              else
                let text = "Некорректный ввод, попробуйте еще раз"
                Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000 ]

      | Employer.AskingFinish employer ->
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {employer.FirstName}
            Фамилия : {employer.LastName}
            Оффис   : {employer.OfficeName}"
          [ Keyboard.create [ Button.create "Принять" (fun _ ->
                                UpdateMessage.FinishEmployerAuth employer
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []

    let authManger mauth =

      match mauth with

      | Manager.EnteringLastFirstName ->
        RenderView.create
          "Введите имя и фамилию"
          [ ctx.BackCancelKeyboard ]
          [ fun message ->
              let array = message.Text.Value.Split(' ')

              if array.Length = 2 then
                let firstName, lastName = array[0], array[1]

                let manager: ManagerDto =
                  { FirstName = firstName
                    LastName = lastName
                    ChatId = message.Chat.Id }

                manager
                |> Manager.AskingFinish
                |> UpdateMessage.AuthManagerChange
                |> ctx.Dispatch
              else
                let text = "Некорректный ввод, попробуйте еще раз"
                Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000 ]

      | Manager.AskingFinish manager ->
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {manager.FirstName}
            Фамилия : {manager.LastName}"
          [ Keyboard.create [ Button.create "Принять" (fun _ ->
                                UpdateMessage.FinishManagerAuth manager
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []

    match authModel with
    | Model.Employer eauth -> authEmployer eauth
    | Model.Manager mauth -> authManger mauth
    | Model.NoAuth ->
      RenderView.create
        "Авторизоваться как менеджер или как сотрудник"
        [ Keyboard.create [ Button.create "Менеджер" (fun _ ->
                              Manager.EnteringLastFirstName
                              |> UpdateMessage.AuthManagerChange
                              |> ctx.Dispatch)
                            Button.create "Сотрудник" (fun _ ->
                              Employer.EnteringOffice
                              |> UpdateMessage.AuthEmployerChange
                              |> ctx.Dispatch) ]
          ctx.BackCancelKeyboard ]
        []

  let view env (history: System.Collections.Generic.Stack<_>) dispatch model =

    let backCancelKeyboard =
      Keyboard.create [ if history.Count > 0 then
                          Button.create "Назад" (fun _ -> dispatch UpdateMessage.Back)
                        if history.Count > 1 then
                          Button.create "Отмена" (fun _ -> dispatch UpdateMessage.Cancel) ]

    let ctx =
      { Dispatch = dispatch
        BackCancelKeyboard = backCancelKeyboard
        AppEnv = env }

    match model with
    | CoreModel.Error message ->
      RenderView.create
        $"Произошла ошибка при инициализации
          Error: {message}"
        [ Keyboard.create [ Button.create "Обновить" (fun _ -> ctx.Dispatch UpdateMessage.Cancel) ] ]
        []
    | CoreModel.Employer employerState -> employerProcess ctx employerState
    | CoreModel.Manager managerState ->
      match managerState.Model with
      | Model.DeAuthEmployers _ ->
        let employers =
          query {
            for employer in Cache.getEmployers ctx.AppEnv do
              where (Cache.isApprovedEmployer ctx.AppEnv employer)
              select employer
          }

        let buttonHandler employer _ =
          match Cache.tryUpdateEmployerApprovedInDb ctx.AppEnv employer false with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация сотрудника успешно убрана"
            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        RenderView.create
          "Выберите сотрудника для которого хотите убрать авторизацию"
          [ for employer in employers do
              Keyboard.create [ Button.create
                                  $"{employer.FirstName} {employer.LastName}"
                                  (buttonHandler employer) ]
            ctx.BackCancelKeyboard ]
          []

      | Model.AuthEmployers _ ->

        let employers =
          query {
            for employer in Cache.getEmployers ctx.AppEnv do
              where (
                Cache.isApprovedEmployer ctx.AppEnv employer
                |> not
              )

              select employer
          }
          |> List.ofSeq

        let buttonHandler employer _ =
          match Cache.tryUpdateEmployerApprovedInDb ctx.AppEnv employer true with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация прошла успешно"
            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        RenderView.create
          "Выберите сотрудника которого хотите авторизовать"
          [ for employer in employers do
              Keyboard.create [ Button.create
                                  $"{employer.FirstName} {employer.LastName}"
                                  (buttonHandler employer) ]
            ctx.BackCancelKeyboard ]
          []

      | Model.InOffice office ->

        let asEmployerState =
          let asEmployer =
            { FirstName = managerState.Manager.FirstName
              LastName = managerState.Manager.LastName
              ChatId = managerState.Manager.ChatId
              Office = office }

          { Employer = asEmployer
            Model = Model.WaitChoice }

        RenderView.create
          $"
          {office.Manager.FirstName} {office.Manager.LastName}
          Меню офиса: {office.OfficeName}"
          [ Keyboard.create [ Button.create "Авторизовать сотрудника" (fun _ ->
                                UpdateMessage.StartAuthEmployers(managerState, office)
                                |> ctx.Dispatch) ]
            Keyboard.create [ Button.create "Убрать авторизацию сотрудника" (fun _ ->
                                UpdateMessage.StartDeAuthEmployers(managerState, office)
                                |> ctx.Dispatch) ]
            Keyboard.create [ Button.create "Создать офис" (fun _ ->
                                UpdateMessage.ManagerMakeOfficeChange(
                                  managerState,
                                  MakeOffice.EnteringName
                                )
                                |> ctx.Dispatch)
                              Button.create "Удалить офис" (fun _ ->
                                match Cache.tryDeleteOffice ctx.AppEnv office.OfficeId with
                                | true ->
                                  let text = $"Офис {office.OfficeName} успешно удален"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    env
                                    office.Manager.ChatId
                                    text
                                    3000

                                  ctx.Dispatch UpdateMessage.Cancel
                                | false ->
                                  let text =
                                    "Нет возможности удалить офис,
                                      возможно с офисом уже связаны какая либо запись либо сотрудник"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    env
                                    office.Manager.ChatId
                                    text
                                    5000) ]
            Keyboard.create [ let createExcelTableFromItemsAsBytes items =
                                let headers =
                                  [ "Имя"
                                    "Серийный номер"
                                    "Мак адрес"
                                    "Куда или для чего"
                                    "Количество"
                                    "Сотрудник"
                                    "Дата" ]

                                [ for head in headers do
                                    FsExcel.Cell [ FsExcel.String head ]
                                  FsExcel.Go FsExcel.NewRow
                                  for item in items do
                                    let count = let c = item.Count in c.GetValue
                                    FsExcel.Cell [ FsExcel.String %item.Item.Name ]
                                    FsExcel.Cell [ FsExcel.String(Option.string item.Item.Serial) ]

                                    FsExcel.Cell [ FsExcel.String(
                                                     Option.string item.Item.MacAddress
                                                   ) ]

                                    FsExcel.Cell [ FsExcel.String(Option.string item.Location) ]
                                    FsExcel.Cell [ FsExcel.Integer(int count) ]

                                    FsExcel.Cell [ FsExcel.String(
                                                     sprintf
                                                       "%s %s"
                                                       %item.Employer.FirstName
                                                       %item.Employer.LastName
                                                   ) ]

                                    FsExcel.Cell [ FsExcel.DateTime item.Time ]
                                    FsExcel.Go FsExcel.NewRow
                                  FsExcel.AutoFit FsExcel.AllCols ]
                                |> FsExcel.Render.AsStreamBytes

                              Button.create "Получить таблицу актуальных записей" (fun _ ->
                                let items =
                                  query {
                                    for item in Cache.getDeletionItems ctx.AppEnv do
                                      where (
                                        item.IsDeletion |> not
                                        && item.IsHidden |> not
                                        && item.Employer.Office.OfficeId = office.OfficeId
                                      )

                                      select item
                                  }
                                  |> List.ofSeq

                                if items.Length > 0 then
                                  try
                                    let streamWithDocument =
                                      let bytes = createExcelTableFromItemsAsBytes items
                                      new System.IO.MemoryStream(bytes)

                                    let documentName =
                                      let dateString =
                                        let now = System.DateTime.Now
                                        $"{now.Day}.{now.Month}.{now.Year} {now.Hour}:{now.Minute}:{now.Second}"

                                      Logger.debug
                                        ctx.AppEnv
                                        $"Generated string from datetime for document is {dateString}"

                                      let name = "ActualItemsTable" + dateString + ".xlsx"

                                      Logger.debug
                                        ctx.AppEnv
                                        $"Generated document name of items is {name}"

                                      name

                                    Utils.sendDocumentAndDeleteAfterDelay
                                      env
                                      managerState.Manager.ChatId
                                      documentName
                                      streamWithDocument
                                      90000

                                    let text =
                                      "Файл отправлен, сообщение с ним будет удалено спустя 90 секунд"

                                    Utils.sendMessageAndDeleteAfterDelay
                                      ctx.AppEnv
                                      managerState.Manager.ChatId
                                      text
                                      5000
                                  with
                                  | exn ->
                                    let text =
                                      $"Произошла ошибка во время создания таблицы {exn.Message}"

                                    Utils.sendMessageAndDeleteAfterDelay
                                      ctx.AppEnv
                                      managerState.Manager.ChatId
                                      text
                                      5000

                                    Logger.error
                                      ctx.AppEnv
                                      $"Exception when try send document excel message = {exn.Message}
                                          Trace: {exn.StackTrace}"
                                else
                                  let text = "Не обнаружено актуальных записей для создания таблицы"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    ctx.AppEnv
                                    managerState.Manager.ChatId
                                    text
                                    5000

                                UpdateMessage.ReRender |> ctx.Dispatch) ]
            Keyboard.create [ Button.create "Добавить запись" (fun _ ->
                                UpdateMessage.DeletionProcessChange(
                                  asEmployerState,
                                  Deletion.EnteringName
                                )
                                |> ctx.Dispatch)
                              Button.create "Удалить запись" (fun _ ->
                                asEmployerState
                                |> UpdateMessage.StartEditDeletionItems
                                |> ctx.Dispatch) ]
            Keyboard.create [ Button.create "Списать все записи" (fun _ ->
                                match Cache.trySetDeletionOnItemsOfOffice ctx.AppEnv office.OfficeId
                                  with
                                | true ->
                                  let text = "Операция прошла успешно"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    env
                                    office.Manager.ChatId
                                    text
                                    3000
                                | false ->
                                  let text = $"Не удалось списать записи, попробуйте попозже"

                                  Utils.sendMessageAndDeleteAfterDelay
                                    env
                                    office.Manager.ChatId
                                    text
                                    5000

                                ctx.Dispatch UpdateMessage.ReRender) ]
            ctx.BackCancelKeyboard ]
          []

      | Model.ChooseOffice offices ->

        RenderView.create
          "Выберите офис"
          [ for office in offices do
              Keyboard.create [

                                Button.create %office.OfficeName (fun _ ->
                                  UpdateMessage.ManagerChooseOffice(managerState, office)
                                  |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []

      | Model.NoOffices ->
        RenderView.create
          "У вас еще не создано ни одного офиса, создайте его для начала работы"
          [ Keyboard.create [ Button.create "Создать офис" (fun _ ->
                                UpdateMessage.ManagerMakeOfficeChange(
                                  managerState,
                                  MakeOffice.EnteringName
                                )
                                |> ctx.Dispatch) ]
            ctx.BackCancelKeyboard ]
          []

      | Model.MakeOffice mprocess ->
        match mprocess with
        | MakeOffice.EnteringName ->
          RenderView.create
            "Введите название офиса"
            [ ctx.BackCancelKeyboard ]
            [ fun message ->

                let officeName: OfficeName = %message.Text.Value

                let dispatch () =

                  let office = Record.createOffice officeName managerState.Manager.ChatId

                  UpdateMessage.ManagerMakeOfficeChange(
                    managerState,
                    MakeOffice.AskingFinish office
                  )
                  |> ctx.Dispatch

                let rec officeAlreadyExist officeName offices =
                  match offices with
                  | head :: tail ->
                    if OfficeName.equals officeName head.OfficeName then
                      true
                    else
                      officeAlreadyExist officeName tail
                  | [] -> false

                let offices = Cache.getOffices ctx.AppEnv

                if officeAlreadyExist officeName offices then
                  let text = "Офис с таким названием уже существует, попробуйте другое"

                  Utils.sendMessageAndDeleteAfterDelay
                    ctx.AppEnv
                    managerState.Manager.ChatId
                    text
                    3000
                else
                  dispatch () ]

        | MakeOffice.AskingFinish office ->
          RenderView.create
            $"Все ли правильно
              Название офиса: {office.OfficeName}"
            [ Keyboard.create [ Button.create "Внести" (fun _ ->
                                  UpdateMessage.FinishMakeOfficeProcess office
                                  |> ctx.Dispatch) ]
              ctx.BackCancelKeyboard ]
            []

    | CoreModel.Auth auth -> authProcess ctx auth
