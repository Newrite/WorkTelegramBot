namespace WorkTelegram.Telegram

open FSharp.UMX

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open Elmish
open WorkTelegram.Telegram
open WorkTelegram.Telegram.ManagerProcess

module View =

  exception private ViewUnmatchedException of string

  open AuthProcess
  open ManagerProcess
  open EmployerProcess

  [<NoEquality>]
  [<NoComparison>]
  type private ViewContext<'ElmishCommand, 'CacheCommand> =
    { Dispatch: UpdateMessage -> unit
      BackCancelKeyboard: Keyboard
      AppEnv: IAppEnv<'ElmishCommand, 'CacheCommand>
      Notify: ChatId -> string -> int -> unit }

  [<RequireQualifiedAccess>]
  module private Functions =

    let sendExcelItemsDocumentExn ctx managerState items =
      let streamWithDocument =
        let bytes = DeletionItem.createExcelTableFromItemsAsByte items
        new System.IO.MemoryStream(bytes)

      let documentName =
        let dateString =
          let now = System.DateTime.Now
          $"{now.Day}.{now.Month}.{now.Year} {now.Hour}:{now.Minute}:{now.Second}"

        Logger.debug ctx.AppEnv $"Generated string from datetime for document is {dateString}"
        let name = "ActualItemsTable" + dateString + ".xlsx"
        Logger.debug ctx.AppEnv $"Generated document name of items is {name}"
        name

      let delay = 90000

      match Utils.sendDocument ctx.AppEnv managerState.Manager.ChatId documentName streamWithDocument with
      | Ok message ->
        let updatedItems = List.map (fun item -> { item with DeletionItem.IsReadyToDeletion = true }) items
        match Repository.tryUpdateDeletionItems ctx.AppEnv updatedItems with
        | true ->
          let text = "Файл отправлен, сообщение с ним будет удалено спустя 90 секунд"
          ctx.Notify managerState.Manager.ChatId text 5000

          task {
            do! Async.Sleep(delay)
            Utils.deleteMessageBase ctx.AppEnv message |> ignore
          }
        |> ignore
        | false ->
          let text = $"Произошла ошибка обращения в базу данных, перезапросите файл еще раз"
          ctx.Notify managerState.Manager.ChatId text 5000
      | Error _ -> ()

      streamWithDocument.Dispose()

    let enteringLastFirstNameEmployerMessageHandle ctx office (message: TelegramMessage) =
      let array = message.Text.Value.Split(' ')

      if array.Length = 2 then
        let lastName, firstName: LastName * FirstName = %array[0], %array[1]

        let employer = Employer.create firstName lastName office %message.Chat.Id

        employer
        |> AuthEmployer.AskingFinish
        |> UpdateMessage.AuthEmployerChange
        |> ctx.Dispatch
      else

      let text = "Некорректный ввод, попробуйте еще раз"
      ctx.Notify %message.Chat.Id text 3000

    let enteringOfficeNameMessageHandle ctx managerState (message: TelegramMessage) =

      let officeName: OfficeName = %message.Text.Value

      let dispatch () =

        let office = Office.create officeName managerState.Manager

        UpdateMessage.ManagerMakeOfficeChange(managerState, MakeOffice.AskingFinish office)
        |> ctx.Dispatch

      let offices = Repository.offices ctx.AppEnv

      if Map.exists(fun _ office -> OfficeName.equals officeName office.OfficeName) offices then
        let text = "Офис с таким названием уже существует, попробуйте другое"

        ctx.Notify managerState.Manager.ChatId text 3000
      else
        dispatch ()

    let enteringLastFirstNameManagerMessageHandle ctx (message: TelegramMessage) =
      let array = message.Text.Value.Split(' ')

      if array.Length = 2 then
        let lastName, firstName: LastName * FirstName = %array[0], %array[1]

        let manager = Manager.create %message.Chat.Id firstName lastName

        manager
        |> AuthManager.AskingFinish
        |> UpdateMessage.AuthManagerChange
        |> ctx.Dispatch
      else

      let text = "Некорректный ввод, попробуйте еще раз"
      ctx.Notify %message.Chat.Id text 3000

    let enteringNameMessageHandle employerState ctx (message: TelegramMessage) =
      let name: ItemName = % message.Text.Value.ToUpper()

      UpdateMessage.DeletionProcessChange(
        employerState,
        ItemWithOnlyName.create name
        |> Deletion.EnteringSerial
      )
      |> ctx.Dispatch

    let enteringSerialMessageHandle
      ctx
      employerState
      (item: ItemWithOnlyName)
      (message: TelegramMessage)
      =
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
      (message: TelegramMessage)
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
        ctx.Notify %message.Chat.Id text 3000

    let enteringCountMessageHandleExn ctx employerState item (message: TelegramMessage) =
      match PositiveInt.tryParse message.Text.Value with
      | Ok pint ->
        UpdateMessage.DeletionProcessChange(employerState, Deletion.EnteringLocation(item, pint))
        |> ctx.Dispatch
      | Error err ->

      match err with
      | BusinessError.NumberMustBePositive incorrectNumber ->
        ctx.Notify
          %message.Chat.Id
          $"Значение должно быть больше нуля, попробуйте еще раз: Введенное значение {incorrectNumber}"
          3000
      | BusinessError.IncorrectParsePositiveNumber incorrectString ->
        ctx.Notify
          %message.Chat.Id
          $"Некорректный ввод, попробуйте еще раз: Введенное значение {incorrectString}"
          3000
      | _ ->
        ViewUnmatchedException($"Unmatched error in view function: error {err}")
        |> raise

    let enteringLocationMessageHandle
      ctx
      employerState
      (item: Item)
      count
      (message: TelegramMessage)
      =
      let location: Location option = %message.Text.Value |> Some

      let deletionItem = DeletionItem.create item count location employerState.Employer

      UpdateMessage.DeletionProcessChange(employerState, Deletion.AskingFinish(deletionItem))
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
        
      let showLastRecords ctx employerState =

        Keyboard.createSingle "Показать последние записи" (fun _ ->
          UpdateMessage.ShowLastDeletionItems employerState
          |> ctx.Dispatch)
        
      let forceInspireItems ctx office =
        Keyboard.createSingle "Подготовить все записи для списания" (fun _ ->
          let items =
            Repository.deletionItems ctx.AppEnv 
            |> Map.toList 
            |> List.map snd
            |> List.filter (fun item -> item.Employer.Office.OfficeId = office.OfficeId)
            |> List.filter (fun item -> not item.IsHidden && not item.IsReadyToDeletion && not item.IsDeletion)
            |> List.filter (fun item -> DeletionItem.inspiredItem System.DateTime.Now item |> not)

          if items.Length > 0 then
            let updatedItems = List.map (fun item -> { item with DeletionItem.IsReadyToDeletion = true }) items
            match Repository.tryUpdateDeletionItems ctx.AppEnv updatedItems with
            | true ->
              let text = "Операция прошла успешно"

              ctx.Notify office.Manager.ChatId text 3000
            | false ->
              let text = "Не удалось подготовить записи, попробуйте попозже"

              ctx.Notify office.Manager.ChatId text 5000

          else
            let text = "Нет записей для подготовки"

            ctx.Notify office.Manager.ChatId text 5000

          ctx.Dispatch UpdateMessage.ReRender)

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
          let deletionItem = DeletionItem.create item count None employerState.Employer

          UpdateMessage.DeletionProcessChange(employerState, Deletion.AskingFinish(deletionItem))
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
            | Some macaddress -> macaddress.Value
            | None -> "_"

          $"{item.Item.Name} - Serial {serial} - Mac {mac}"

        Keyboard.createSingle text (fun _ ->
          match Repository.tryUpdateDeletionItem ctx.AppEnv {item with IsHidden = true} with
          | true ->
            let text = "Запись успешно удалена"
            ctx.Notify employerState.Employer.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

          | false ->
            let text = "Не удалось удалить запись, попробуйте еще раз или попозже"
            ctx.Notify employerState.Employer.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender)
        
      let showDeletionItem ctx item =
        let text =
          let serial =
            match item.Item.Serial with
            | Some serial -> %serial
            | None -> "_"
            
          let item_date = item.Time.Date

          $"Date:{item_date} N:{item.Item.Name} S:{serial}"

        Keyboard.createSingle text (fun _ ->
            ctx.Dispatch UpdateMessage.ReRender)

      let renderOffice office onClick = Keyboard.createSingle %office.OfficeName onClick

      let accept onClick = Keyboard.createSingle "Принять" onClick

      let noAuthManager ctx =
        Keyboard.createSingle "Менеджер" (fun _ ->
          AuthManager.EnteringLastFirstName
          |> UpdateMessage.AuthManagerChange
          |> ctx.Dispatch)

      let noAuthEmployer ctx =
        Keyboard.createSingle "Сотрудник" (fun _ ->
          AuthEmployer.EnteringOffice
          |> UpdateMessage.AuthEmployerChange
          |> ctx.Dispatch)

      let deAuthEmployer ctx (managerState: ManagerContext) employer =

        let onClick employer _ =
          match Repository.tryUpdateEmployer ctx.AppEnv { employer with IsApproved = false } with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            ctx.Notify managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация сотрудника успешно убрана"
            ctx.Notify managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        Keyboard.createSingle $"{employer.FirstName} {employer.LastName}" (onClick employer)

      let authEmployer ctx (managerState: ManagerContext) employer =

        let onClick employer _ =
          match Repository.tryUpdateEmployer ctx.AppEnv { employer with IsApproved = true } with
          | false ->
            let text =
              "Произошла ошибка во время изменения авторизации сотрудника, попробуйте еще раз"

            ctx.Notify managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Авторизация прошла успешно"
            ctx.Notify managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.ReRender

        Keyboard.createSingle $"{employer.FirstName} {employer.LastName}" (onClick employer)
        
      let delegateEmployerChooseOffice ctx (managerState: ManagerContext) employer choosenOffice office =

        let onClick employer _ =
          match Repository.tryUpdateEmployer ctx.AppEnv { employer with Office =  choosenOffice; IsApproved = false } with
          | false ->
            let text =
              "Произошла ошибка во время изменения офиса сотрудника, попробуйте еще раз"

            ctx.Notify managerState.Manager.ChatId text 5000
            ctx.Dispatch UpdateMessage.ReRender
          | true ->
            let text = "Офис сотрудника изменен успешно"
            ctx.Notify managerState.Manager.ChatId text 3000
            EventBus.removeFromDictEvent ctx.AppEnv employer.ChatId
            ctx.Dispatch <| UpdateMessage.Back
            ctx.Dispatch <| UpdateMessage.Back

        Keyboard.createSingle $"{choosenOffice.OfficeName}" (onClick employer)
        
      let delegateEmployer ctx (managerState: ManagerContext) employer office =

        let onClick employer _ =            
          ctx.Dispatch <| UpdateMessage.DelegateEmployerChoose(managerState, office, employer)

        Keyboard.createSingle $"{employer.FirstName} {employer.LastName}" (onClick employer)
        
      let delegateOffice ctx (managerState: ManagerContext) (manager: Manager) office =

        let onClick manager _ =
         // match Repository.tryUpdateOffice ctx.AppEnv { office with Manager = manager } with
         // | false ->
         //   let text =
         //     "Произошла ошибка во время изменения менеджера офиса, попробуйте еще раз"

         //   ctx.Notify managerState.Manager.ChatId text 5000
         //   ctx.Dispatch UpdateMessage.ReRender
         // | true ->
         //   let text = "Смена менеджера офиса прошла успешно"
         //   ctx.Notify managerState.Manager.ChatId text 3000
         //   EventBus.removeFromDictEvent ctx.AppEnv manager.ChatId
         //   EventBus.removeFromDictEvent ctx.AppEnv managerState.Manager.ChatId
         //   ctx.Dispatch UpdateMessage.ReRender
            
          ctx.Dispatch <| UpdateMessage.FinishDelegateOffice(managerState, manager, office)

        Keyboard.createSingle $"{manager.FirstName} {manager.LastName}" (onClick manager)

      let managerMenuAuthEmployer ctx managerState office =
        Keyboard.createSingle "Авторизовать сотрудника" (fun _ ->
          UpdateMessage.StartAuthEmployers(managerState, office)
          |> ctx.Dispatch)

      let managerMenuDeAuthEmployer ctx managerState office =
        Keyboard.createSingle "Убрать авторизацию сотрудника" (fun _ ->
          UpdateMessage.StartDeAuthEmployers(managerState, office)
          |> ctx.Dispatch)
        
      let managerMenuEmployerOperations ctx managerState office =
        Keyboard.createSingle "Действия с сотрудниками" (fun _ ->
          UpdateMessage.StartEmployerOperations(managerState, office)
          |> ctx.Dispatch)
        
      let managerMenuDelegateEmployer ctx managerState office =
        Keyboard.createSingle "Переместить сотрдника офиса в другой офис" (fun _ ->
          UpdateMessage.StartDelegateEmployer(managerState, office)
          |> ctx.Dispatch)

      let managerMenuOfficesOperations ctx managerState office =
        Keyboard.create [ Button.create "Создать офис" (fun _ ->
                            UpdateMessage.ManagerMakeOfficeChange(
                              managerState,
                              MakeOffice.EnteringName
                            )
                            |> ctx.Dispatch)
                          Button.create "Удалить офис" (fun _ ->
                            match Repository.tryDeleteOffice ctx.AppEnv office with
                            | true ->
                              let text = $"Офис {office.OfficeName} успешно удален"

                              ctx.Notify
                                office.Manager.ChatId
                                text
                                3000

                              ctx.Dispatch UpdateMessage.Cancel
                            | false ->
                              let text =
                                "Нет возможности удалить офис,
                    возможно с офисом уже связаны какая либо запись либо сотрудник"

                              ctx.Notify
                                office.Manager.ChatId
                                text
                                5000

                              ctx.Dispatch UpdateMessage.ReRender) ]

      let managerMenuGetExcelTableOfActualItems ctx managerState office =

        Keyboard.createSingle "Получить таблицу актуальных записей" (fun _ ->

          let items = 
            Repository.deletionItems ctx.AppEnv 
            |> Map.toList 
            |> List.map snd
            |> List.filter (fun item -> item.Employer.Office.OfficeId = office.OfficeId)
            |> List.filter (fun item -> not item.IsHidden && not item.IsDeletion)
            |> List.filter (fun item -> DeletionItem.inspiredItem System.DateTime.Now item || item.IsReadyToDeletion)

          if items.Length > 0 then
            try

              Functions.sendExcelItemsDocumentExn ctx managerState items

            with
            | exn ->
              let text = $"Произошла ошибка во время создания таблицы {exn.Message}"

              ctx.Notify managerState.Manager.ChatId text 5000

              Logger.error
                ctx.AppEnv
                $"Exception when try send document excel message = {exn.Message}
                    Trace: {exn.StackTrace}"
          else

          let text = "Не обнаружено актуальных записей для создания таблицы"

          ctx.Notify managerState.Manager.ChatId text 5000

          UpdateMessage.ReRender |> ctx.Dispatch)
        
      let managerMenuGetExcelTableOfAllItems ctx managerState office =

        Keyboard.createSingle "Получить таблицу всех записей" (fun _ ->

          let items = 
            Repository.deletionItems ctx.AppEnv 
            |> Map.toList 
            |> List.map snd
            |> List.filter (fun item -> item.Employer.Office.OfficeId = office.OfficeId)

          if items.Length > 0 then
            try

              Functions.sendExcelItemsDocumentExn ctx managerState items

            with
            | exn ->
              let text = $"Произошла ошибка во время создания таблицы {exn.Message}"

              ctx.Notify managerState.Manager.ChatId text 5000

              Logger.error
                ctx.AppEnv
                $"Exception when try send document excel message = {exn.Message}
                    Trace: {exn.StackTrace}"
          else

          let text = "Не обнаружено актуальных записей для создания таблицы"

          ctx.Notify managerState.Manager.ChatId text 5000

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

      let managerMenuDeletionAllItemRecords ctx office =
        Keyboard.createSingle "Списать все записи" (fun _ ->
          let items =
            Repository.deletionItems ctx.AppEnv 
            |> Map.toList 
            |> List.map snd
            |> List.filter (fun item -> item.Employer.Office.OfficeId = office.OfficeId)
            |> List.filter (fun item -> not item.IsHidden && item.IsReadyToDeletion && not item.IsDeletion)
            |> List.filter (DeletionItem.inspiredItem System.DateTime.Now)

          if items.Length > 0 then
            let updatedItems = List.map (fun item -> { item with DeletionItem.IsDeletion = true }) items
            match Repository.tryUpdateDeletionItems ctx.AppEnv updatedItems with
            | true ->
              let text = "Операция прошла успешно"

              ctx.Notify office.Manager.ChatId text 3000
            | false ->
              let text = "Не удалось списать записи, попробуйте попозже"

              ctx.Notify office.Manager.ChatId text 5000

          else
            let text = "Нет записей для списания"

            ctx.Notify office.Manager.ChatId text 5000

          ctx.Dispatch UpdateMessage.ReRender)
        
      let managerMenuDelegateOffice (ctx: ViewContext<'a, CacheCommand>) (managerState: ManagerContext) (office: Office) =
        Keyboard.createSingle "Передать оффис" (fun _ ->
            UpdateMessage.StartDelegateOffice(managerState, office)
            |> ctx.Dispatch)

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
            Keyboard.showLastRecords ctx employerState
            Keyboard.forceInspireItems ctx employerState.Employer.Office
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
          
      let showRecords ctx employerState items =
        RenderView.create
          "Последние записи"
          [ for item in items do
              Keyboard.showLastRecords ctx employerState
            ctx.BackCancelKeyboard ]

      let renderOffices ctx (offices: OfficesMap) onClick =
        RenderView.create
          "Выберите офис из списка"
          [ for office in offices do
              Keyboard.renderOffice office.Value (onClick office.Value)
            ctx.BackCancelKeyboard ]

      let employerAuthAskingFinish ctx (employer: Types.Employer) onClick =
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {employer.FirstName}
            Фамилия : {employer.LastName}
            Офис    : {employer.Office.OfficeName}"
          [ Keyboard.accept onClick
            ctx.BackCancelKeyboard ]

      let managerAuthAskingFinish ctx (manager: Types.Manager) onClick =
        RenderView.create
          $"Авторизоваться с этими данными?
            Имя     : {manager.FirstName}
            Фамилия : {manager.LastName}"
          [ Keyboard.accept onClick
            ctx.BackCancelKeyboard ]

      let noAuth ctx =
        RenderView.create
          "Авторизоваться как менеджер или как сотрудник"
          [ Keyboard.noAuthManager ctx
            Keyboard.noAuthEmployer ctx
            ctx.BackCancelKeyboard ]

      let deAuthEmployers ctx managerState (employers: EmployersMap) =
        RenderView.create
          "Выберите сотрудника для которого хотите убрать авторизацию"
          [ for employer in employers do
              Keyboard.deAuthEmployer ctx managerState employer.Value
            ctx.BackCancelKeyboard ]

      let authEmployers ctx managerState (employers: EmployersMap) =
        RenderView.create
          "Выберите сотрудника которого хотите авторизовать"
          [ for employer in employers do
              Keyboard.authEmployer ctx managerState employer.Value
            ctx.BackCancelKeyboard ]
          
      let delegateOffice ctx managerState (managers: ManagersMap) office =
        RenderView.create
          "Выберите менеджера которому хотите передать оффис"
          [ for manager in managers do
              Keyboard.delegateOffice ctx managerState manager.Value office
            ctx.BackCancelKeyboard ]
          
      let delegateEmployerChooseOffice ctx managerState employer (offices: OfficesMap) office =
        RenderView.create
          $"Сотрудник: {employer.FirstName} {employer.LastName}\nВыберите офис в который хотите переместить сотрудника"
          [ for office_from in offices do
              Keyboard.delegateEmployerChooseOffice ctx managerState employer office_from.Value office
            ctx.BackCancelKeyboard ]
          
      let delegateEmployer ctx managerState (employers: EmployersMap) office =
        RenderView.create
          "Выберите сотрудника которого хотите переместить"
          [ for employer in employers do
              Keyboard.delegateEmployer ctx managerState employer.Value office
            ctx.BackCancelKeyboard ]
          
      let employerOperations ctx managerState office =
        RenderView.create
          "Выберите действие"
          [ Keyboard.managerMenuAuthEmployer ctx managerState office
            Keyboard.managerMenuDeAuthEmployer ctx managerState office
            Keyboard.managerMenuDelegateEmployer ctx managerState office
            ctx.BackCancelKeyboard ]
          
      let managerOfficeOperations ctx managerState office =
        RenderView.create
          "Выберите действие"
          [ Keyboard.managerMenuOfficesOperations ctx managerState office
            Keyboard.managerMenuDelegateOffice ctx managerState office
            ctx.BackCancelKeyboard ]

      let managerMenuInOffice ctx managerState (office: Office) asEmployerState =
        RenderView.create
          $"
          {office.Manager.FirstName} {office.Manager.LastName}
          Меню офиса: {office.OfficeName}"
          // [ Keyboard.managerMenuAuthEmployer ctx managerState office
          //   Keyboard.managerMenuDeAuthEmployer ctx managerState office
          [ Keyboard.managerMenuEmployerOperations ctx managerState office
            Keyboard.managerMenuOfficesOperations ctx managerState office
            Keyboard.managerMenuDelegateOffice ctx managerState office
            Keyboard.managerMenuAddEditItemRecord ctx asEmployerState
            Keyboard.managerMenuGetExcelTableOfActualItems ctx managerState office
            Keyboard.managerMenuGetExcelTableOfAllItems ctx managerState office
            Keyboard.managerMenuDeletionAllItemRecords ctx office
            ctx.BackCancelKeyboard ]

      let makeOfficeForStartWork ctx managerState =
        RenderView.create
          "У вас еще не создано ни одного офиса, создайте его для начала работы"
          [ Keyboard.startMakeOfficeProcess ctx managerState
            ctx.BackCancelKeyboard ]

      let finishMakeOffice ctx (office: Office) =
        RenderView.create
          $"Все ли правильно
            Название офиса: {office.OfficeName}"
          [ Keyboard.createOffice ctx office
            ctx.BackCancelKeyboard ]

      let coreModelCatchError ctx message =
        RenderView.create
          $"Произошла ошибка при инициализации
            Error: {message}"
          [ Keyboard.refresh ctx ]

  [<RequireQualifiedAccess>]
  module private ViewEmployer =

    let waitChoice ctx employerState =
      
      let employer = Repository.tryEmployerByChatId ctx.AppEnv employerState.Employer.ChatId

      if employer.IsSome && employer.Value.IsApproved then

        Forms.RenderView.approvedEmployerMenu ctx {employerState with EmployerContext.Employer = employer.Value } []
      else

        Forms.RenderView.waitingApproveEmployerMenu ctx []

    let editDeletionItems ctx employerState =

      let items =
        
        let currentTime = System.DateTime.Now

        Repository.deletionItems ctx.AppEnv
        |> Map.toList
        |> List.map snd
        |> List.filter (fun item -> item.Employer.Office.OfficeId = employerState.Employer.Office.OfficeId)
        |> List.filter (fun item -> not item.IsHidden && not item.IsReadyToDeletion && not item.IsDeletion)
        |> List.filter (fun item -> DeletionItem.inspiredItem currentTime item |> not)

      if items.Length < 1 then
        RenderView.create
          "Не нашлось записей внесенных за последние 24 часа"
          [ ctx.BackCancelKeyboard ]
          []
      else
        Forms.RenderView.editDeletionItems ctx employerState items []
        
    let showLastDeletionItems ctx employerState =

      let items =
        
        let currentTime = System.DateTime.Now
        
        let mutable counter = 0

        Repository.deletionItems ctx.AppEnv
        |> Map.toList
        |> List.map snd
        |> List.filter (fun item -> item.Employer.Office.OfficeId = employerState.Employer.Office.OfficeId)
        |> List.filter (fun item -> not item.IsHidden)
        |> List.filter (DeletionItem.inspiredItem currentTime)
        |> List.takeWhile (fun _ ->
          counter <- counter + 1
          counter <= 5)

      if items.Length < 1 then
        RenderView.create
          "Не нашлось готовых или уже списанных записей"
          [ ctx.BackCancelKeyboard ]
          []
      else
        Forms.RenderView.showRecords ctx employerState items []

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
        let offices = Repository.offices ctx.AppEnv

        let onClick office _ =
          office
          |> AuthEmployer.EnteringLastFirstName
          |> UpdateMessage.AuthEmployerChange
          |> ctx.Dispatch

        if offices.Count > 0 then
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
      | AuthEmployer.EnteringOffice -> AuthProcess.enteringOffice ctx
      | AuthEmployer.EnteringLastFirstName office -> AuthProcess.enteringLastFirstName ctx office
      | AuthEmployer.AskingFinish recordEmployer -> AuthProcess.askingFinish ctx recordEmployer

  [<RequireQualifiedAccess>]
  module private ViewManager =

    [<RequireQualifiedAccess>]
    module AuthProcess =

      let enteringLastFirstName ctx =
        RenderView.create
          "Введите фамилию и имя"
          [ ctx.BackCancelKeyboard ]
          [ Functions.enteringLastFirstNameManagerMessageHandle ctx ]

      let askingFinish ctx manager =

        let onClick _ =
          UpdateMessage.FinishManagerAuth manager
          |> ctx.Dispatch

        Forms.RenderView.managerAuthAskingFinish ctx manager onClick []

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

    let deAuthEmployers ctx managerState office =
      let employers =
        Repository.employers ctx.AppEnv
        |> Map.filter (fun _ employer -> employer.IsApproved && employer.Office = office)

      Forms.RenderView.deAuthEmployers ctx managerState employers []

    let authEmployers ctx managerState office =
      let employers =
        Repository.employers ctx.AppEnv
        |> Map.filter (fun _ employer -> not employer.IsApproved && employer.Office = office)

      Forms.RenderView.authEmployers ctx managerState employers []
      
    let delegateOffice ctx managerState office =
      let managers =
        Repository.managers ctx.AppEnv
        |> Map.filter (fun chatId _ -> chatId.Equals(managerState.Manager.ChatId) |> not)

      Forms.RenderView.delegateOffice ctx managerState managers office []
      
    let delegateEmployer ctx managerState office =
      let employers =
        Repository.employers ctx.AppEnv
        |> Map.filter (fun _ e -> e.Office = office)

      Forms.RenderView.delegateEmployer ctx managerState employers office []
      
    let delegateEmployerChooseOffice ctx managerState office employer =
      let offices =
        Repository.offices ctx.AppEnv
        |> Map.filter (fun _ o ->
          Logger.debug ctx.AppEnv $"Filtered offices for delegate employer: {o.OfficeId} {o.IsHidden}"
          o.OfficeId <> office.OfficeId && not o.IsHidden )

      Forms.RenderView.delegateEmployerChooseOffice ctx managerState employer offices office []
      
    let employerOperations ctx managerState office =

      Forms.RenderView.employerOperations ctx managerState office []
      
    let officeOperations ctx managerState office =

      Forms.RenderView.employerOperations ctx managerState office []

    let inOffice ctx managerState office =

      let asEmployerState =
        let asEmployer = Manager.asEmployer managerState.Manager office

        { Employer = asEmployer
          Model = EmployerModel.WaitChoice }

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
      | AuthManager.EnteringLastFirstName -> AuthProcess.enteringLastFirstName ctx
      | AuthManager.AskingFinish manager -> AuthProcess.askingFinish ctx manager

  let private employerProcess ctx (employerState: EmployerContext) =

    match employerState.Model with
    | EmployerModel.WaitChoice -> ViewEmployer.waitChoice ctx employerState
    | EmployerModel.Deletion delProcess -> ViewEmployer.deletionProcess ctx employerState delProcess
    | EmployerModel.EditDeletionItems -> ViewEmployer.editDeletionItems ctx employerState
    | EmployerModel.ShowedLastRecords -> ViewEmployer.showLastDeletionItems ctx employerState

  let private managerProcess ctx (managerState: ManagerContext) =
    match managerState.Model with
    | ManagerModel.DeAuthEmployers office -> ViewManager.deAuthEmployers ctx managerState office
    | ManagerModel.AuthEmployers office -> ViewManager.authEmployers ctx managerState office
    | ManagerModel.DelegateOffice office -> ViewManager.delegateOffice ctx managerState office
    | ManagerModel.InOffice office -> ViewManager.inOffice ctx managerState office
    | ManagerModel.ChooseOffice offices -> ViewManager.chooseOffice ctx managerState offices
    | ManagerModel.NoOffices -> ViewManager.noOffices ctx managerState
    | ManagerModel.MakeOffice makeProcess ->
      ViewManager.makeOfficeProcess ctx managerState makeProcess
    | ManagerModel.EmployerOperations office -> ViewManager.employerOperations ctx managerState office
    | ManagerModel.OfficeOperations office -> failwith "todo"
    | ManagerModel.DelegateEmployer office -> ViewManager.delegateEmployer ctx managerState office
    | ManagerModel.DelegateEmployerChooseOffice(office, employer) -> ViewManager.delegateEmployerChooseOffice ctx managerState office employer

  let private authProcess (ctx: ViewContext<_, _>) authModel =

    match authModel with
    | AuthModel.Employer employerAuth -> ViewEmployer.authEmployer ctx employerAuth
    | AuthModel.Manager managerAuth -> ViewManager.authManager ctx managerAuth
    | AuthModel.NoAuth -> Forms.RenderView.noAuth ctx []

  let view env dispatch model =

    let backCancelKeyboard =
      Keyboard.create [ if model.History.Count > 0 then
                          Button.create "Назад" (fun _ -> dispatch UpdateMessage.Back)
                        if model.History.Count > 1 then
                          Button.create "Отмена" (fun _ -> dispatch UpdateMessage.Cancel) ]

    let ctx =
      { Dispatch = dispatch
        BackCancelKeyboard = backCancelKeyboard
        AppEnv = env
        Notify = Utils.sendMessageAndDeleteAfterDelay env }

    match model.Model with
    | CoreModel.Error message -> Forms.RenderView.coreModelCatchError ctx message []
    | CoreModel.Employer employerState -> employerProcess ctx employerState
    | CoreModel.Manager managerState -> managerProcess ctx managerState
    | CoreModel.Auth auth -> authProcess ctx auth
