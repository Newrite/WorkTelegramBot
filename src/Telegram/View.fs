namespace WorkTelegram.Telegram

open FSharp.UMX

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open Elmish
open WorkTelegram.Telegram
open WorkTelegram.Telegram.EmployerProcess
open WorkTelegram.Telegram.ManagerProcess

module View =

  exception private ViewUnmatchedException of string

  open AuthProcess

  [<NoEquality>]
  [<NoComparison>]
  type private ViewContext<'Command> =
    { Dispatch: UpdateMessage -> unit
      BackCancelKeyboard: Keyboard
      AppEnv: IAppEnv<'Command> }

  [<RequireQualifiedAccess>]
  module private Functions =

    type Message = Funogram.Telegram.Types.Message

    let createExcelTableFromItemsAsBytes items =
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

          FsExcel.Cell [ FsExcel.String(Option.string item.Item.MacAddress) ]

          FsExcel.Cell [ FsExcel.String(Option.string item.Location) ]
          FsExcel.Cell [ FsExcel.Integer(int count) ]

          FsExcel.Cell [ FsExcel.String($"{item.Employer.FirstName} {item.Employer.LastName}") ]

          FsExcel.Cell [ FsExcel.DateTime item.Time ]
          FsExcel.Go FsExcel.NewRow
        FsExcel.AutoFit FsExcel.AllCols ]
      |> FsExcel.Render.AsStreamBytes

    let sendExcelItemsDocumentExn ctx managerState items =
      let streamWithDocument =
        let bytes = createExcelTableFromItemsAsBytes items
        new System.IO.MemoryStream(bytes)

      let documentName =
        let dateString =
          let now = System.DateTime.Now
          $"{now.Day}.{now.Month}.{now.Year} {now.Hour}:{now.Minute}:{now.Second}"

        Logger.debug ctx.AppEnv $"Generated string from datetime for document is {dateString}"
        let name = "ActualItemsTable" + dateString + ".xlsx"
        Logger.debug ctx.AppEnv $"Generated document name of items is {name}"
        name

      Utils.sendDocumentAndDeleteAfterDelay
        ctx.AppEnv
        managerState.Manager.ChatId
        documentName
        streamWithDocument
        90000

      let text = "Файл отправлен, сообщение с ним будет удалено спустя 90 секунд"

      Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 5000

    let enteringLastFirstNameEmployerMessageHandle ctx office (message: Message) =
      let array = message.Text.Value.Split(' ')

      if array.Length = 2 then
        let lastName, firstName: LastName * FirstName = %array[0], %array[1]

        let employer =
          Record.createEmployer
            office.OfficeId
            office.OfficeName
            firstName
            lastName
            %message.Chat.Id

        employer
        |> AuthProcess.Employer.AskingFinish
        |> UpdateMessage.AuthEmployerChange
        |> ctx.Dispatch
      else

      let text = "Некорректный ввод, попробуйте еще раз"
      Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000

    let enteringOfficeNameMessageHandle ctx managerState (message: Message) =

      let officeName: OfficeName = %message.Text.Value

      let dispatch () =

        let office = Record.createOffice officeName managerState.Manager.ChatId

        UpdateMessage.ManagerMakeOfficeChange(managerState, MakeOffice.AskingFinish office)
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

        Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 3000
      else
        dispatch ()

    let enteringLastFirstNameManagerMessageHandle ctx (message: Message) =
      let array = message.Text.Value.Split(' ')

      if array.Length = 2 then
        let lastName, firstName = array[0], array[1]

        let manager: ManagerDto =
          { FirstName = firstName
            LastName = lastName
            ChatId = message.Chat.Id }

        manager
        |> AuthProcess.Manager.AskingFinish
        |> UpdateMessage.AuthManagerChange
        |> ctx.Dispatch
      else

      let text = "Некорректный ввод, попробуйте еще раз"
      Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000

    let enteringNameMessageHandle employerState ctx (message: Message) =
      let name: ItemName = % message.Text.Value.ToUpper()

      UpdateMessage.DeletionProcessChange(
        employerState,
        ItemWithOnlyName.create name
        |> Deletion.EnteringSerial
      )
      |> ctx.Dispatch

    let enteringSerialMessageHandle ctx employerState (item: ItemWithOnlyName) (message: Message) =
      let serial: Serial = % message.Text.Value.ToUpper()

      UpdateMessage.DeletionProcessChange(
        employerState,
        ItemWithSerial.create item.Name serial
        |> Deletion.EnteringMac
      )
      |> ctx.Dispatch

    let enteringMacAddressMessageHandle
      ctx
      employerState
      (item: ItemWithSerial)
      (message: Message)
      =
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
        Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv %message.Chat.Id text 3000

    let enteringCountMessageHandleExn ctx employerState item (message: Message) =
      match PositiveInt.tryParse message.Text.Value with
      | Ok pint ->
        UpdateMessage.DeletionProcessChange(employerState, Deletion.EnteringLocation(item, pint))
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
        |> raise

    let enteringLocationMessageHandle ctx employerState (item: Item) count (message: Message) =
      let location: Location option = %message.Text.Value |> Some

      let recordedDeletion =
        Record.createDeletionItem
          item
          count
          System.DateTime.Now
          location
          employerState.Employer.Office.OfficeId
          employerState.Employer.ChatId

      UpdateMessage.DeletionProcessChange(employerState, Deletion.AskingFinish(recordedDeletion))
      |> ctx.Dispatch

  [<RequireQualifiedAccess>]
  module private Forms =

    [<RequireQualifiedAccess>]
    module Keyboard =

      let addRecord ctx employerState =

        Keyboard.createSingle "Добавить запись" (fun _ ->
          UpdateMessage.DeletionProcessChange(employerState, Deletion.EnteringName)
          |> ctx.Dispatch)

      let deleteRecord ctx employerState =

        Keyboard.createSingle "Удалить запись" (fun _ ->
          UpdateMessage.StartEditDeletionItems employerState
          |> ctx.Dispatch)

      let refresh ctx =
        Keyboard.createSingle "Обновить" (fun _ -> ctx.Dispatch UpdateMessage.Cancel)

      let withoutSerial ctx employerState item =
        Keyboard.createSingle "Без серийного номера" (fun _ ->
          UpdateMessage.DeletionProcessChange(
            employerState,
            item
            |> Item.ItemWithOnlyName
            |> Deletion.EnteringCount
          )
          |> ctx.Dispatch)

      let withoutMacAddress ctx employerState item =
        Keyboard.createSingle "Пропустить" (fun _ ->
          UpdateMessage.DeletionProcessChange(
            employerState,
            (item |> Item.ItemWithSerial, PositiveInt.one)
            |> Deletion.EnteringLocation
          )
          |> ctx.Dispatch)

      let withoutLocation ctx employerState item count =
        Keyboard.createSingle "Пропустить" (fun _ ->
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
          |> ctx.Dispatch)

      let enterDeletionItemRecord ctx employerState recordedDeletionItem =
        Keyboard.createSingle "Внести" (fun _ ->
          UpdateMessage.FinishDeletionProcess(employerState, recordedDeletionItem)
          |> ctx.Dispatch)

      let hideDeletionItem ctx employerState item =
        let text =
          let serial =
            match item.Item.Serial with
            | Some serial -> %serial
            | None -> "_"

          let mac =
            match item.Item.MacAddress with
            | Some macaddress -> macaddress.GetValue
            | None -> "_"

          $"{item.Item.Name} - Serial {serial} - Mac {mac}"

        Keyboard.createSingle text (fun _ ->
          match Cache.tryHideDeletionItem ctx.AppEnv item.DeletionId with
          | true ->
            let text = "Запись успешно удалена"
            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv employerState.Employer.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

          | false ->
            let text = "Не удалось удалить запись, попробуйте еще раз или попозже"
            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv employerState.Employer.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender)

      let renderOffice office onClick = Keyboard.createSingle %office.OfficeName onClick

      let accept onClick = Keyboard.createSingle "Принять" onClick

      let noAuthManager ctx =
        Keyboard.createSingle "Менеджер" (fun _ ->
          AuthProcess.Manager.EnteringLastFirstName
          |> UpdateMessage.AuthManagerChange
          |> ctx.Dispatch)

      let noAuthEmployer ctx =
        Keyboard.createSingle "Сотрудник" (fun _ ->
          AuthProcess.Employer.EnteringOffice
          |> UpdateMessage.AuthEmployerChange
          |> ctx.Dispatch)

      let deAuthEmployer ctx (managerState: ManagerContext) employer =

        let onClick employer _ =
          match Cache.tryUpdateEmployerApprovedInDb ctx.AppEnv employer false with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация сотрудника успешно убрана"
            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        Keyboard.createSingle $"{employer.FirstName} {employer.LastName}" (onClick employer)

      let authEmployer ctx (managerState: ManagerContext) employer =

        let onClick employer _ =
          match Cache.tryUpdateEmployerApprovedInDb ctx.AppEnv employer true with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация прошла успешно"
            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        Keyboard.createSingle $"{employer.FirstName} {employer.LastName}" (onClick employer)

      let managerMenuAuthEmployer ctx managerState office =
        Keyboard.createSingle "Авторизовать сотрудника" (fun _ ->
          UpdateMessage.StartAuthEmployers(managerState, office)
          |> ctx.Dispatch)

      let managerMenuDeAuthEmployer ctx managerState office =
        Keyboard.createSingle "Убрать авторизацию сотрудника" (fun _ ->
          UpdateMessage.StartDeAuthEmployers(managerState, office)
          |> ctx.Dispatch)

      let managerMenuOfficesOperations ctx managerState office =
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
                                ctx.AppEnv
                                office.Manager.ChatId
                                text
                                3000

                              ctx.Dispatch UpdateMessage.Cancel
                            | false ->
                              let text =
                                "Нет возможности удалить офис,
                    возможно с офисом уже связаны какая либо запись либо сотрудник"

                              Utils.sendMessageAndDeleteAfterDelay
                                ctx.AppEnv
                                office.Manager.ChatId
                                text
                                5000

                              ctx.Dispatch UpdateMessage.ReRender) ]

      let getExcelTableOfActualItems ctx managerState office =

        Keyboard.createSingle "Получить таблицу актуальных записей" (fun _ ->
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

              Functions.sendExcelItemsDocumentExn ctx managerState items

            with
            | exn ->
              let text = $"Произошла ошибка во время создания таблицы {exn.Message}"

              Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 5000

              Logger.error
                ctx.AppEnv
                $"Exception when try send document excel message = {exn.Message}
                    Trace: {exn.StackTrace}"
          else

          let text = "Не обнаружено актуальных записей для создания таблицы"

          Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv managerState.Manager.ChatId text 5000

          UpdateMessage.ReRender |> ctx.Dispatch)

      let managerMenuAddEditItemRecord ctx asEmployerState =
        let addButton =
          addRecord ctx asEmployerState
          |> fun keyboard -> keyboard.Buttons
          |> Seq.head

        let deleteButton =
          deleteRecord ctx asEmployerState
          |> fun keyboard -> keyboard.Buttons
          |> Seq.head

        Keyboard.create [ addButton; deleteButton ]

      let managerMenuDeletinAllItemRecords ctx office =
        Keyboard.createSingle "Списать все записи" (fun _ ->
          match Cache.trySetDeletionOnItemsOfOffice ctx.AppEnv office.OfficeId with
          | True ->
            let text = "Операция прошла успешно"

            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv office.Manager.ChatId text 3000
          | False ->
            let text = "Не удалось списать записи, попробуйте попозже"

            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv office.Manager.ChatId text 5000

          | Partial ->
            let text = "Нет записей для списания"

            Utils.sendMessageAndDeleteAfterDelay ctx.AppEnv office.Manager.ChatId text 5000

          ctx.Dispatch UpdateMessage.ReRender)

      let startMakeOfficeProcess ctx managerState =
        Keyboard.createSingle "Создать офис" (fun _ ->
          UpdateMessage.ManagerMakeOfficeChange(managerState, MakeOffice.EnteringName)
          |> ctx.Dispatch)

      let createOffice ctx recordOffice =
        Keyboard.createSingle "Создать" (fun _ ->
          UpdateMessage.FinishMakeOfficeProcess recordOffice
          |> ctx.Dispatch)

    [<RequireQualifiedAccess>]
    module RenderView =

      let approvedEmployerMenu ctx employerState =
        let text =
          sprintf
            "Привет %s %s из %s"
            %employerState.Employer.FirstName
            %employerState.Employer.LastName
            %employerState.Employer.Office.OfficeName

        RenderView.create
          text
          [ Keyboard.addRecord ctx employerState
            Keyboard.deleteRecord ctx employerState
            ctx.BackCancelKeyboard ]

      let waitingApproveEmployerMenu ctx =
        let text =
          "Ваша учетная запись еще не авторизована менеджером офис которого вы выбрали"

        RenderView.create text [ Keyboard.refresh ctx ]

      let delProcessEnteringSerial ctx employerState item =
        let text = "Введите серийный номер"

        RenderView.create
          text
          [ Keyboard.withoutSerial ctx employerState item
            ctx.BackCancelKeyboard ]

      let delProcessEnteringMacAddress ctx employerState item =
        let text = "Введите мак адрес"

        RenderView.create
          text
          [ Keyboard.withoutMacAddress ctx employerState item
            ctx.BackCancelKeyboard ]

      let delProcessEnteringLocation ctx employerState item count =
        let text = "Введите для чего либо куда"

        RenderView.create
          text
          [ Keyboard.withoutLocation ctx employerState item count
            ctx.BackCancelKeyboard ]

      let delProcessAskingFinish ctx employerState recordedDeletionItem =
        let text =
          $"Внести эту запись?
           {recordedDeletionItem.ToString()}"

        RenderView.create
          text
          [ Keyboard.enterDeletionItemRecord ctx employerState recordedDeletionItem
            ctx.BackCancelKeyboard ]

      let editDeletionItems ctx employerState items =
        RenderView.create
          "Выберите запись для удаления"
          [ for item in items do
              Keyboard.hideDeletionItem ctx employerState item
            ctx.BackCancelKeyboard ]

      let renderOffices ctx offices onClick =
        RenderView.create
          "Выберите офис из списка"
          [ for office in offices do
              Keyboard.renderOffice office (onClick office)
            ctx.BackCancelKeyboard ]

      let employerAuthAskingFinish ctx (employerRecord: RecordEmployer) onClick =
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {employerRecord.FirstName}
            Фамилия : {employerRecord.LastName}
            Офис    : {employerRecord.OfficeName}"
          [ Keyboard.accept onClick
            ctx.BackCancelKeyboard ]

      let managerAuthAskingFinish ctx (managerDto: ManagerDto) onClick =
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {managerDto.FirstName}
            Фамилия : {managerDto.LastName}"
          [ Keyboard.accept onClick
            ctx.BackCancelKeyboard ]

      let noAuth ctx =
        RenderView.create
          "Авторизоваться как менеджер или как сотрудник"
          [ Keyboard.noAuthManager ctx
            Keyboard.noAuthEmployer ctx
            ctx.BackCancelKeyboard ]

      let deAuthEmployers ctx managerState employers =
        RenderView.create
          "Выберите сотрудника для которого хотите убрать авторизацию"
          [ for employer in employers do
              Keyboard.deAuthEmployer ctx managerState employer
            ctx.BackCancelKeyboard ]

      let authEmployers ctx managerState employers =
        RenderView.create
          "Выберите сотрудника которого хотите авторизовать"
          [ for employer in employers do
              Keyboard.authEmployer ctx managerState employer
            ctx.BackCancelKeyboard ]

      let managerMenuInOffice ctx managerState (office: Office) asEmployerState =
        RenderView.create
          $"
          {office.Manager.FirstName} {office.Manager.LastName}
          Меню офиса: {office.OfficeName}"
          [ Keyboard.managerMenuAuthEmployer ctx managerState office
            Keyboard.managerMenuDeAuthEmployer ctx managerState office
            Keyboard.managerMenuOfficesOperations ctx managerState office
            Keyboard.managerMenuAddEditItemRecord ctx asEmployerState
            Keyboard.managerMenuDeletinAllItemRecords ctx office
            ctx.BackCancelKeyboard ]

      let makeOfficeForStartWork ctx managerState =
        RenderView.create
          "У вас еще не создано ни одного офиса, создайте его для начала работы"
          [ Keyboard.startMakeOfficeProcess ctx managerState
            ctx.BackCancelKeyboard ]

      let finishMakeOffice ctx (recordOffice: RecordOffice) =
        RenderView.create
          $"Все ли правильно
            Название офиса: {recordOffice.OfficeName}"
          [ Keyboard.createOffice ctx recordOffice
            ctx.BackCancelKeyboard ]

      let coreModelCatchError ctx message =
        RenderView.create
          $"Произошла ошибка при инициализации
            Error: {message}"
          [ Keyboard.refresh ctx ]

  [<RequireQualifiedAccess>]
  module private ViewEmployer =

    let waitChoice ctx employerState =

      if Cache.isApprovedEmployer ctx.AppEnv employerState.Employer then

        Forms.RenderView.approvedEmployerMenu ctx employerState []
      else

        Forms.RenderView.waitingApproveEmployerMenu ctx []

    let editDeletionItems ctx employerState =

      let items =

        let notInspired time =
          let since: System.TimeSpan = System.DateTime.Now - time
          since.TotalHours < 24.

        query {
          for item in Cache.getDeletionItems ctx.AppEnv do
            where (
              item.Employer.ChatId = employerState.Employer.ChatId
              && notInspired item.Time
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
        Forms.RenderView.editDeletionItems ctx employerState items []

    [<RequireQualifiedAccess>]
    module DeletionProcess =

      let enteringName ctx employerState =
        RenderView.create
          "Введите имя записи"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringNameMessageHandle employerState ctx ]

      let enteringSerial ctx employerState item =
        Forms.RenderView.delProcessEnteringSerial
          ctx
          employerState
          item
          [ Functions.enteringSerialMessageHandle ctx employerState item ]

      let enteringMacAddress ctx employerState item =
        Forms.RenderView.delProcessEnteringMacAddress
          ctx
          employerState
          item
          [ Functions.enteringMacAddressMessageHandle ctx employerState item ]

      let enteringCount ctx employerState item =
        RenderView.create
          "Введите количество"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringCountMessageHandleExn ctx employerState item ]

      let enteringLocation ctx employerState item count =
        Forms.RenderView.delProcessEnteringLocation
          ctx
          employerState
          item
          count
          [ Functions.enteringLocationMessageHandle ctx employerState item count ]

      let askingFinish ctx employerState recordedDeletionItem =
        Forms.RenderView.delProcessAskingFinish ctx employerState recordedDeletionItem []

    [<RequireQualifiedAccess>]
    module AuthProcess =

      let enteringOffice ctx =
        let offices = Cache.getOffices ctx.AppEnv

        let onClick office _ =
          office
          |> Employer.EnteringLastFirstName
          |> UpdateMessage.AuthEmployerChange
          |> ctx.Dispatch

        if offices.Length > 0 then
          Forms.RenderView.renderOffices ctx offices onClick []
        else
          RenderView.create "Нет офисов" [ ctx.BackCancelKeyboard ] []

      let enteringLastFirstName ctx office =
        RenderView.create
          "Введите фамилию и имя"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringLastFirstNameEmployerMessageHandle ctx office ]

      let askingFinish ctx employerRecord =

        let onClick _ =
          UpdateMessage.FinishEmployerAuth employerRecord
          |> ctx.Dispatch

        Forms.RenderView.employerAuthAskingFinish ctx employerRecord onClick []

    let deletionProcess ctx employerState (delProcess: Deletion) =
      match delProcess with
      | Deletion.EnteringName -> DeletionProcess.enteringName ctx employerState
      | Deletion.EnteringSerial item -> DeletionProcess.enteringSerial ctx employerState item
      | Deletion.EnteringMac item -> DeletionProcess.enteringMacAddress ctx employerState item
      | Deletion.EnteringCount item -> DeletionProcess.enteringCount ctx employerState item
      | Deletion.EnteringLocation (item, count) ->
        DeletionProcess.enteringLocation ctx employerState item count
      | Deletion.AskingFinish recordedDeletionItem ->
        DeletionProcess.askingFinish ctx employerState recordedDeletionItem

    let authEmployer ctx employerAuth =
      match employerAuth with
      | Employer.EnteringOffice -> AuthProcess.enteringOffice ctx
      | Employer.EnteringLastFirstName office -> AuthProcess.enteringLastFirstName ctx office
      | Employer.AskingFinish recordEmployer -> AuthProcess.askingFinish ctx recordEmployer

  [<RequireQualifiedAccess>]
  module private ViewManager =

    [<RequireQualifiedAccess>]
    module AuthProcess =

      let enteringLastFirstName ctx =
        RenderView.create
          "Введите фамилию и имя"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringLastFirstNameManagerMessageHandle ctx ]

      let askingFinish ctx managerDto =

        let onClick _ =
          UpdateMessage.FinishManagerAuth managerDto
          |> ctx.Dispatch

        Forms.RenderView.managerAuthAskingFinish ctx managerDto onClick []

    [<RequireQualifiedAccess>]
    module MakeOffice =

      let enteringName ctx managerState =
        RenderView.create
          "Введите название офиса"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringOfficeNameMessageHandle ctx managerState ]

      let askingFinish ctx recordOffice =
        Forms.RenderView.finishMakeOffice ctx recordOffice []

    let makeOfficeProcess ctx managerState makeProcess =
      match makeProcess with
      | MakeOffice.EnteringName -> MakeOffice.enteringName ctx managerState
      | MakeOffice.AskingFinish recordOffice -> MakeOffice.askingFinish ctx recordOffice

    let deAuthEmployers ctx managerState =
      let employers =
        query {
          for employer in Cache.getEmployers ctx.AppEnv do
            where (Cache.isApprovedEmployer ctx.AppEnv employer)
            select employer
        }

      Forms.RenderView.deAuthEmployers ctx managerState employers []

    let authEmployers ctx managerState =
      let employers =
        query {
          for employer in Cache.getEmployers ctx.AppEnv do
            where (
              Cache.isApprovedEmployer ctx.AppEnv employer
              |> not
            )

            select employer
        }

      Forms.RenderView.authEmployers ctx managerState employers []

    let inOffice ctx managerState office =

      let asEmployerState =
        let asEmployer = Manager.asEmployer managerState.Manager office

        { Employer = asEmployer
          Model = Model.WaitChoice }

      Forms.RenderView.managerMenuInOffice ctx managerState office asEmployerState []

    let noOffices ctx managerState =
      Forms.RenderView.makeOfficeForStartWork ctx managerState []

    let chooseOffice ctx managerState offices =

      let onClick office _ =
        UpdateMessage.ManagerChooseOffice(managerState, office)
        |> ctx.Dispatch

      Forms.RenderView.renderOffices ctx offices onClick []

    let authManager ctx managerAuth =
      match managerAuth with
      | Manager.EnteringLastFirstName -> AuthProcess.enteringLastFirstName ctx
      | Manager.AskingFinish managerDto -> AuthProcess.askingFinish ctx managerDto

  let private employerProcess ctx (employerState: EmployerContext) =

    match employerState.Model with
    | Model.WaitChoice -> ViewEmployer.waitChoice ctx employerState
    | Model.Deletion delProcess -> ViewEmployer.deletionProcess ctx employerState delProcess
    | Model.EditDeletionItems -> ViewEmployer.editDeletionItems ctx employerState

  let private managerProcess ctx managerState =
    match managerState.Model with
    | Model.DeAuthEmployers _ -> ViewManager.deAuthEmployers ctx managerState
    | Model.AuthEmployers _ -> ViewManager.authEmployers ctx managerState
    | Model.InOffice office -> ViewManager.inOffice ctx managerState office
    | Model.ChooseOffice offices -> ViewManager.chooseOffice ctx managerState offices
    | Model.NoOffices -> ViewManager.noOffices ctx managerState
    | Model.MakeOffice makeProcess -> ViewManager.makeOfficeProcess ctx managerState makeProcess

  let private authProcess (ctx: ViewContext<_>) authModel =

    match authModel with
    | Model.Employer employerAuth -> ViewEmployer.authEmployer ctx employerAuth
    | Model.Manager managerAuth -> ViewManager.authManager ctx managerAuth
    | Model.NoAuth -> Forms.RenderView.noAuth ctx []

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
    | CoreModel.Error message -> Forms.RenderView.coreModelCatchError ctx message []
    | CoreModel.Employer employerState -> employerProcess ctx employerState
    | CoreModel.Manager managerState -> managerProcess ctx managerState
    | CoreModel.Auth auth -> authProcess ctx auth
