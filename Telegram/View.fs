namespace WorkTelegram.Telegram

open FSharp.UMX

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open Elmish

module View =

  open EmployerProcess
  open ManagerProcess
  open AuthProcess

  [<NoEquality>]
  [<NoComparison>]
  type private ViewContext = 
    { Dispatch:           UpdateMessage -> unit
      BackCancelKeyboard: Keyboard
      Env:                Env }
  

  let private employerProcess (ctx: ViewContext) (employerState: EmployerProcess.EmployerContext) =

    let waitChoice() =

      if Database.isApproved ctx.Env employerState.Employer then

        let text =
          sprintf "Привет %s %s из %s"
            %employerState.Employer.FirstName
            %employerState.Employer.LastName
            %employerState.Employer.Office.OfficeName

        RenderView.create text [
          Keyboard.create [
            Button.create 
              "Добавить запись"                    
              (fun _ -> UpdateMessage.DeletionProcessChange(employerState, Deletion.EnteringName) |> ctx.Dispatch )
          ]
          Keyboard.create [
            Button.create 
              "Отредактировать записи"
              (fun _ -> employerState |> UpdateMessage.StartEditRecordedItems |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] []

      else

        let text =
          "Ваша учетная запись еще не авторизована менеджером офис которого вы выбрали"

        RenderView.create text [
          Keyboard.create [
            Button.create "Обновить" (fun _ -> ctx.Dispatch UpdateMessage.Cancel)
          ]
        ] []

    let deletionProcess (dprocess: EmployerProcess.Deletion)=

      match dprocess with
      | Deletion.EnteringName ->

        RenderView.create "Введите имя записи" [
          ctx.BackCancelKeyboard
        ] [ fun message ->
              let name :ItemName = %message.Text.Value.ToUpper()
              UpdateMessage.DeletionProcessChange(
                employerState,
                ItemWithOnlyName.create name |> Deletion.EnteringSerial)
              |> ctx.Dispatch ]

      | Deletion.EnteringSerial item ->

        RenderView.create "Введите серийный номер" [
          Keyboard.create [
            Button.create
              "Без серийного номера"
              (fun _ ->
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  item |> Item.ItemWithOnlyName |> Deletion.EnteringCount)
                |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] [ fun message ->
              let serial :Serial = %message.Text.Value.ToUpper()
              UpdateMessage.DeletionProcessChange(
                employerState,
                ItemWithSerial.create item.Name serial |> Deletion.EnteringMac)
              |> ctx.Dispatch ]

      | Deletion.EnteringMac item ->

        RenderView.create "Введите мак адрес" [
          Keyboard.create [
            Button.create
              "Пропустить"
              (fun _ ->
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  (item |> Item.ItemWithSerial, PositiveInt.one) |> Deletion.EnteringLocation)
                |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] [ fun message ->
              match MacAddress.validate (message.Text.Value.ToUpper()) with
              | Ok macaddress ->
                let itemWithMacaddress = Item.createWithMacAddress item.Name item.Serial macaddress
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  Deletion.EnteringLocation(itemWithMacaddress, PositiveInt.one))
                |> ctx.Dispatch 
              | Error _ ->
                 let text = "Некорректный ввод мак адреса, попробуйте еще раз"
                 Utils.sendMessageAndDeleteAfterDelay ctx.Env %message.Chat.Id text 3000 ]

      | Deletion.EnteringCount item ->

        RenderView.create "Введите количество" [
          ctx.BackCancelKeyboard
        ] [ fun message ->
              match PositiveInt.tryParse(message.Text.Value) with
              | Ok    pint ->
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  Deletion.EnteringLocation(item, pint))
                |> ctx.Dispatch
              | Error err  ->
                match err with
                | PositiveIntError.NumberMustBePositive ->
                  Utils.sendMessageAndDeleteAfterDelay
                    ctx.Env %message.Chat.Id "Значение должно быть больше нуля, попробуйте еще раз" 3000
                | PositiveIntError.CantParseStringToPositiveInt ->
                  Utils.sendMessageAndDeleteAfterDelay
                    ctx.Env %message.Chat.Id "Некорректный ввод, попробуйте еще раз" 3000 ]

      | Deletion.EnteringLocation (item, count) ->

        RenderView.create "Введите для чего либо куда" [
          Keyboard.create [
            Button.create 
              "Пропустить"
              (fun _ ->
                let recordedDeletion =
                  { Item     = item
                    Count    = count
                    Time     = System.DateTime.Now
                    Location = None 
                    Employer = employerState.Employer }
                UpdateMessage.DeletionProcessChange(
                  employerState,
                  Deletion.AskingFinish(recordedDeletion))
                |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] [ fun message ->
              let location: Location option = %message.Text.Value |> Some
              let recordedDeletion =
                { Item     = item
                  Count    = count
                  Time     = System.DateTime.Now
                  Location = location 
                  Employer = employerState.Employer }
              UpdateMessage.DeletionProcessChange(
                employerState,
                Deletion.AskingFinish(recordedDeletion))
              |> ctx.Dispatch ]

      | Deletion.AskingFinish recordedDeletionItem ->

        let messageText =
          $"Внести эту запись?
           {recordedDeletionItem.ToString()}"

        RenderView.create messageText [
          Keyboard.create [
            Button.create 
              "Внести"
              (fun _ -> UpdateMessage.FinishDeletionProcess(employerState, recordedDeletionItem, ctx.Env) |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] []

    match employerState.Model with
    | Model.WaitChoice -> waitChoice()

    | Model.Deletion dprocess ->
      deletionProcess dprocess

    | Model.EditRecordedDeletions ->
        let items = Database.selectDeletionItems ctx.Env employerState.Employer
        if items.Length < 1 then
          RenderView.create "Не нашлось записей внесенных за последние 24 часа" [
            ctx.BackCancelKeyboard
          ] []
        else
          RenderView.create "Выберите запись для удаления" [
            for (item, itemId) in items do
              Keyboard.create [
                let text =
                  let serial = 
                    match item.Item.Serial with
                    | Some serial -> %serial
                    | None        -> "_"
                  let mac    =
                    match item.Item.MacAddress with
                    | Some macaddress -> %macaddress
                    | None            -> "_"
                  $"{item.Item.Name} - Serial {serial} - Mac {mac}"
                Button.create 
                  text
                  (fun _ -> 
                    match Database.setIsHiddenTrueForItem ctx.Env itemId with
                    | Ok _      -> ctx.Dispatch UpdateMessage.Back
                    | Error err ->
                      match err with
                      | DatabaseError.CantDeleteRecordedItem _
                      | DatabaseError.SQLiteException _ ->
                        let text = 
                          $"Error when try delete item with id {itemId}"
                        Utils.sendMessageAndDeleteAfterDelay ctx.Env employerState.Employer.ChatId text 5000
                        ctx.Dispatch UpdateMessage.Back
                      | _ -> 
                        ctx.Dispatch UpdateMessage.Back)
              ]
            ctx.BackCancelKeyboard
          ] []


  let private authProcess (ctx: ViewContext) authModel =

    let authEmployer eauth =
  
      let renderOffices (offices: RecordedOffice list) =

        if offices.Length > 0 then

          RenderView.create "Выберите офис из списка" [
            for office in offices do
              Keyboard.create [
                Button.create 
                  %office.OfficeName
                  (fun _ -> office |> Employer.EnteringLastFirstName |> UpdateMessage.AuthEmployerChange |> ctx.Dispatch)
              ]
            ctx.BackCancelKeyboard
            ] []

        else

          RenderView.create "Нет офисов" [ ctx.BackCancelKeyboard ] []
  
      match eauth with

      | Employer.EnteringOffice ->
        match Cache.offices ctx.Env with
          | Some offices -> renderOffices offices
          | None ->
            RenderView.create 
              "Не удалось получить данные из кеша" [ ctx.BackCancelKeyboard ] []

      | Employer.EnteringLastFirstName office ->
        RenderView.create "Введите имя и фамилию" [
          ctx.BackCancelKeyboard
        ] [ fun message -> 
              let array = message.Text.Value.Split(' ')
              if array.Length = 2 then
                let firstName, lastName: FirstName * LastName = %array[0], %array[1]
                let employer = 
                  RecordedEmployer.create firstName lastName office %message.Chat.Id
                employer |> Employer.AskingFinish |> UpdateMessage.AuthEmployerChange |> ctx.Dispatch
              else
                let text = "Некорректный ввод, попробуйте еще раз"
                Utils.sendMessageAndDeleteAfterDelay ctx.Env %message.Chat.Id text 3000
              ]

      | Employer.AskingFinish employer ->
        RenderView.create 
          $"Авторизоваться с этими данными?
            Имя     : {employer.FirstName}
            Фамилия : {employer.LastName}
            Оффис   : {employer.Office.OfficeName}" [
              Keyboard.create [
                Button.create
                  "Принять" (fun _ -> UpdateMessage.FinishEmployerAuth(employer, ctx.Env) |> ctx.Dispatch)
              ]
              ctx.BackCancelKeyboard
            ] []

    let authManger mauth =
  
      match mauth with

      | Manager.EnteringLastFirstName ->
        RenderView.create "Введите имя и фамилию" [
          ctx.BackCancelKeyboard
        ] [ fun message -> 
              let array = message.Text.Value.Split(' ')
              if array.Length = 2 then
                let firstName, lastName: FirstName * LastName = %array[0], %array[1]
                let manager: RecordedManager =
                  { FirstName = firstName
                    LastName  = lastName 
                    ChatId    = %message.Chat.Id }
                manager |> Manager.AskingFinish |> UpdateMessage.AuthManagerChange |> ctx.Dispatch
              else
                let text = "Некорректный ввод, попробуйте еще раз"
                Utils.sendMessageAndDeleteAfterDelay ctx.Env %message.Chat.Id text 3000
              ]

      | Manager.AskingFinish manager ->
        RenderView.create 
          $"Авторизоваться с этими данными?
            Имя     : {manager.FirstName}
            Фамилия : {manager.LastName}" [
              Keyboard.create [
                Button.create
                  "Принять" (fun _ -> UpdateMessage.FinishManagerAuth(manager, ctx.Env) |> ctx.Dispatch)
              ]
              ctx.BackCancelKeyboard
            ] []
  
    match authModel with
    | Model.Employer eauth -> authEmployer eauth
    | Model.Manager  mauth -> authManger   mauth
    | Model.NoAuth ->
      RenderView.create "Авторизоваться как менеджер или как сотрудник" [ 
        Keyboard.create [
          Button.create 
            "Менеджер"   
            (fun _ -> Manager.EnteringLastFirstName |> UpdateMessage.AuthManagerChange  |> ctx.Dispatch)
          Button.create 
            "Сотрудник"
            (fun _ -> Employer.EnteringOffice       |> UpdateMessage.AuthEmployerChange |> ctx.Dispatch)
        ]
        ctx.BackCancelKeyboard
      ] []

  let view (env: Env) (history: System.Collections.Generic.Stack<_>) dispatch model =

    let backCancelKeyboard =
      Keyboard.create [
        if history.Count > 0 then
          Button.create "Назад"  (fun _ -> dispatch UpdateMessage.Back)
        if history.Count > 1 then
          Button.create "Отмена" (fun _ -> dispatch UpdateMessage.Cancel)
      ]

    let ctx =
      { Dispatch           = dispatch
        BackCancelKeyboard = backCancelKeyboard
        Env                = env }
    
    match model with
    | CoreModel.Error message ->
      RenderView.create 
        $"Произошла ошибка при инициализации
          Error: {message}" [
            Keyboard.create [
              Button.create "Обновить" (fun _ -> ctx.Dispatch UpdateMessage.Cancel)
            ]
          ] []
    | CoreModel.Employer employerState -> employerProcess ctx employerState
    | CoreModel.Manager  managerState  -> 
      match managerState.Model with
      | Model.DeAuthEmployers office ->
        let employers =
          match Cache.officeEmployers env office with
          | Some employers -> 
            employers 
            |> List.filter (fun e -> Database.isApproved env e)
          | None -> []

        let buttonHandler employer _ =
          match Database.updateIsApprovedEmployer env false employer with
          | Error err ->
            match err with 
            | DatabaseError.CantUpdateEmployerApproved _ ->
              let text =
                $"Database Error: {DatabaseError.CantUpdateEmployerApproved}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
            | DatabaseError.SQLiteException exn ->
              let text =
                $"Database Error: {exn.Message}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
            | err ->
              let text =
                $"Database Error: {err}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
          | Ok _ ->
            let text =
              "Авторизация сотрудника успешно убрана"
            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.Back

        RenderView.create "Выберите сотрудника для которого хотите убрать авторизацию" [
          for employer in employers do
            Keyboard.create [
              Button.create
                $"{employer.FirstName} {employer.LastName}"
                (buttonHandler employer)
            ]
          ctx.BackCancelKeyboard
        ] []

      | Model.AuthEmployers office ->

        let employers =
          match Cache.officeEmployers env office with
          | Some employers -> 
            employers 
            |> List.filter (fun e -> Database.isApproved env e |> not)
          | None -> []

        let buttonHandler employer _ =
          match Database.updateIsApprovedEmployer env true employer with
          | Error err ->
            match err with 
            | DatabaseError.CantUpdateEmployerApproved _ ->
              let text =
                $"Database Error: {DatabaseError.CantUpdateEmployerApproved}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
            | DatabaseError.SQLiteException exn ->
              let text =
                $"Database Error: {exn.Message}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
            | err ->
              let text =
                $"Database Error: {err}"
              Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 5000
              ctx.Dispatch UpdateMessage.Back
          | Ok _ ->
            let text =
              "Сотрудник успешно авторизован"
            Utils.sendMessageAndDeleteAfterDelay env managerState.Manager.ChatId text 3000
            ctx.Dispatch UpdateMessage.Back

        RenderView.create "Выберите сотрудника которого хотите авторизовать" [
          for employer in employers do
            Keyboard.create [
              Button.create
                $"{employer.FirstName} {employer.LastName}"
                (buttonHandler employer)
            ]
          ctx.BackCancelKeyboard
        ] []

      | Model.InOffice office ->
        
        let asEmployerState =
          let asEmployer =
            { FirstName = managerState.Manager.FirstName
              LastName  = managerState.Manager.LastName
              ChatId    = managerState.Manager.ChatId
              Office    = office }
          { Employer = asEmployer
            Model    = Model.WaitChoice}

        RenderView.create $"
          {office.Manager.FirstName} {office.Manager.LastName}
          Меню офиса: {office.OfficeName}" [
          Keyboard.create [
            Button.create 
              "Авторизовать сотрудника"
              (fun _ -> UpdateMessage.StartAuthEmployers(managerState, office) |> ctx.Dispatch)
          ]
          Keyboard.create [
            Button.create 
              "Убрать авторизацию сотрудника"
              (fun _ -> UpdateMessage.StartDeAuthEmployers(managerState, office) |> ctx.Dispatch)
          ]
          Keyboard.create [
            Button.create 
              "Создать офис"
              (fun _ -> 
                UpdateMessage.ManagerMakeOfficeChange(
                  managerState, 
                  MakeOffice.EnteringName)
                |> ctx.Dispatch)
            Button.create
              "Удалить офис"
              (fun _ ->
                match Database.tryDeleteOfficeByOfficeNameAndUpdateCache env office with
                | Ok _      ->
                  let text = 
                    $"Офис {office.OfficeName} успешно удален"
                  Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 3000
                  ctx.Dispatch UpdateMessage.Cancel
                | Error err ->
                  let text =
                    $"Нет возможности удалить офис, возможно с офисом уже связаны какая либо запись либо сотрудник"
                  Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 5000)
          ]
          Keyboard.create [
            let createExcelTableFromItemsAsBytes items =
              let headers = ["Имя"; "Серийный номер"; "Мак адрес"; "Куда или для чего"; "Количество"; "Сотрудник"; "Дата"]
              [ for head in headers do
                  FsExcel.Cell [ FsExcel.String head ]
                FsExcel.Go FsExcel.NewRow
                for item in items do
                  let count = let c = item.Count in c.GetValue
                  FsExcel.Cell [ FsExcel.String   %item.Item.Name                                                   ]
                  FsExcel.Cell [ FsExcel.String   (Option.string item.Item.Serial                                  )]
                  FsExcel.Cell [ FsExcel.String   (Option.string item.Item.MacAddress                              )]
                  FsExcel.Cell [ FsExcel.String   (Option.string item.Location                                     )]
                  FsExcel.Cell [ FsExcel.Integer  (int count                                                       )]
                  FsExcel.Cell [ FsExcel.String   (sprintf "%s %s" %item.Employer.FirstName %item.Employer.LastName)]
                  FsExcel.Cell [ FsExcel.DateTime item.Time                                                         ]
                  FsExcel.Go FsExcel.NewRow
                FsExcel.AutoFit FsExcel.AllCols
              ] |> FsExcel.Render.AsStreamBytes

            Button.create 
              "Получить таблицу актуальных записей"                   
              (fun _ ->
                match Database.selectAllActualItemsByOffice env office with
                | Ok items ->
                  if items.Length > 0 then
                    try
                      let streamWithDocument =
                        let bytes = createExcelTableFromItemsAsBytes items
                        new System.IO.MemoryStream(bytes)
                      let documentName =
                        let dateString =
                          let now = System.DateTime.Now
                          $"{now.Day}.{now.Month}.{now.Year} {now.Hour}:{now.Minute}:{now.Second}"
                        ctx.Env.Log.Debug $"Generated string from datetime for document is {dateString}"
                        let name = "ActualItemsTable" + dateString + ".xlsx"
                        ctx.Env.Log.Debug $"Generated document name of items is {name}"
                        name
                      Utils.sendDocumentAndDeleteAfterDelay env managerState.Manager.ChatId documentName streamWithDocument 90000
                      let text =
                        "Файл отправлен, сообщение с ним будет удалено спустя 90 секунд"
                      Utils.sendMessageAndDeleteAfterDelay ctx.Env managerState.Manager.ChatId text 5000
                    with exn ->
                      let text =
                        $"Произошла ошибка во время создания таблицы {exn.Message}"
                      Utils.sendMessageAndDeleteAfterDelay ctx.Env managerState.Manager.ChatId text 5000
                      ctx.Env.Log.Error
                        $"Exception when try send document excel message = {exn.Message}
                          Trace: {exn.StackTrace}"
                  else
                  let text =
                    $"Не обнаружено актуальных записей для создания таблицы"
                  Utils.sendMessageAndDeleteAfterDelay ctx.Env managerState.Manager.ChatId text 5000
                | Error err ->
                  let text =
                    $"Произошла ошибка во время сбора записей {err}"
                  Utils.sendMessageAndDeleteAfterDelay ctx.Env managerState.Manager.ChatId text 5000
                UpdateMessage.NothingChange |> ctx.Dispatch )
          ]
          Keyboard.create [
            Button.create 
              "Добавить запись"                   
              (fun _ -> UpdateMessage.DeletionProcessChange(asEmployerState, Deletion.EnteringName) |> ctx.Dispatch )
            Button.create 
              "Удалить запись" 
              (fun _ -> asEmployerState |> UpdateMessage.StartEditRecordedItems |> ctx.Dispatch)
          ]
          Keyboard.create [
            Button.create 
              "Списать все записи" 
              (fun _ ->
                match Database.setIsDeletionTrueForAllItemsInOffice env office with
                | Ok deleted ->
                  let text =
                    if deleted > 0 then
                      $"Успешно списано {deleted} записей"
                    else
                      "Нет записей для списния"
                  Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 3000
                | Error err ->
                  match err with
                  | DatabaseError.SQLiteException exn ->
                    let text =
                      $"Произошла ошибка при попытке списать записи: {exn.Message}"
                    Utils.sendMessageAndDeleteAfterDelay env office.Manager.ChatId text 5000
                  | _ -> ())
          ]
          ctx.BackCancelKeyboard
        ] []

      | Model.ChooseOffice offices ->
        
        RenderView.create "Выберите офис" [
          for office in offices do
            Keyboard.create [

              Button.create %office.OfficeName
                (fun _ -> UpdateMessage.ManagerChooseOffice(managerState, office) |> ctx.Dispatch)
            ]
          ctx.BackCancelKeyboard
        ] []

      | Model.NoOffices ->
        RenderView.create "У вас еще не создано ни одного офиса, создайте его для начала работы" [
          Keyboard.create [
            Button.create 
              "Создать офис"
              (fun _ -> 
                UpdateMessage.ManagerMakeOfficeChange(
                  managerState, 
                  MakeOffice.EnteringName)
                |> ctx.Dispatch)
          ]
          ctx.BackCancelKeyboard
        ] []

      | Model.MakeOffice mprocess ->
        match mprocess with
        | MakeOffice.EnteringName ->
          RenderView.create "Введите название офиса" [
            ctx.BackCancelKeyboard
          ] [ fun message ->

                let officeName: OfficeName = %message.Text.Value

                let dispatch() =

                  let office =
                    { OfficeName = officeName
                      Manager    = managerState.Manager }

                  UpdateMessage.ManagerMakeOfficeChange(
                    managerState,
                    MakeOffice.AskingFinish office)
                  |> ctx.Dispatch

                let rec officeAlreadyExist officeName offices =
                  match offices with
                  | head :: tail ->
                    if OfficeName.equals officeName head.OfficeName then
                      true
                    else
                      officeAlreadyExist officeName tail
                  | [] -> false

                match Cache.offices ctx.Env with
                | Some offices ->
                  if officeAlreadyExist officeName offices then
                    let text =
                      "Офис с таким названием уже существует, попробуйте другое"
                    Utils.sendMessageAndDeleteAfterDelay ctx.Env managerState.Manager.ChatId text 3000
                  else
                    dispatch()
                | None -> dispatch() ]

        | MakeOffice.AskingFinish office ->
          RenderView.create 
            $"Все ли правильно
              Название офиса: {office.OfficeName}" [
            Keyboard.create [
              Button.create "Внести"
                (fun _ -> UpdateMessage.FinishMakeOfficeProcess(office, env) |> ctx.Dispatch)
            ]
            ctx.BackCancelKeyboard
          ] []

    | CoreModel.Auth auth -> authProcess ctx auth