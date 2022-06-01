namespace WorkTelegram.Core

open FSharp.UMX
open Funogram.Types
open Microsoft.Data.Sqlite
open System

[<AutoOpen>]
module UMX =
  [<Measure>]
  type private itemname

  [<Measure>]
  type private serialnumber

  [<Measure>]
  type private macaddress

  [<Measure>]
  type private lastname

  [<Measure>]
  type private firstname

  [<Measure>]
  type private officename

  [<Measure>]
  type private location

  [<Measure>]
  type private chatid

  type ItemName = string<itemname>
  type Serial = string<serialnumber>
  type MacAddress = string<macaddress>
  type LastName = string<lastname>
  type FirstName = string<firstname>
  type OfficeName = string<officename>
  type Location = string<location>
  type ChatId = int64<chatid>

open UMX

[<RequireQualifiedAccess>]
module Option =

  let string optionValue =
    match optionValue with
    | Some v -> string v
    | None -> "None"


[<AutoOpen>]
module rec Types =

  [<RequireQualifiedAccess>]
  type PositiveIntError =
    | NumberMustBePositive
    | CantParseStringToPositiveInt

  [<RequireQualifiedAccess>]
  type MacAddressError = | CantValidateMacAddress

  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type DatabaseError =
    | CantInsertEmployer of RecordedEmployer
    | CantInsertManager of RecordedManager
    | CantInsertOffice of RecordedOffice
    | CantDeleteOffice of RecordedOffice
    | ChatIdAlreadyExistInDatabase of ChatId
    | CantUpdateEmployerApproved of RecordedEmployer
    | CantDeleteRecordedItem of int64
    | SQLiteException of Microsoft.Data.Sqlite.SqliteException
    | UnknownException of System.Exception

  [<RequireQualifiedAccess>]
  type AppError =
    | PositiveIntError of PositiveIntError
    | MacAddressError of MacAddressError

  [<RequireQualifiedAccess>]
  module MacAddress =

    let validate (input: string) : Result<MacAddress, MacAddressError> =

      let rec format (chars: char []) (acc: string) counter position =
        if position > 11 then
          acc
        elif counter = 2 then
          let newWord = acc + ":"
          format chars newWord 0 position
        else

        let newWord = acc + (string chars[position])
        let newPosition = position + 1
        let newCounter = counter + 1
        format chars newWord newCounter newPosition


      let inputCleaned =
        input
          .Replace(" ", "")
          .Replace(":", "")
          .Replace("-", "")
          .Replace(";", "")

      let r = new Text.RegularExpressions.Regex("[a-fA-F0-9]{12}$")

      match r.IsMatch(inputCleaned) with
      | true ->
        let macAddress = format (inputCleaned.ToCharArray()) "" 0 0
        %macAddress |> Ok
      | false -> MacAddressError.CantValidateMacAddress |> Error

  [<RequireQualifiedAccess>]
  module OfficeName =

    let equals officeOne officeTwo =

      let officeString (officeName: OfficeName) =
        let str: string = %officeName

        str
          .ToLower()
          .Replace(" ", "")
          .Replace(":", "")
          .Replace(";", "")
          .Replace("-", "")

      let officeStringOne = officeString officeOne
      let officeStringTwo = officeString officeTwo

      officeStringOne = officeStringTwo

  [<Struct>]
  type PositiveInt =
    private
      { Value: uint }

    member self.GetValue = self.Value

  [<RequireQualifiedAccess>]
  module PositiveInt =

    let create count =
      if count > 0u then
        { Value = count } |> Ok
      else
        Error PositiveIntError.NumberMustBePositive

    let tryParse (str: string) =
      match UInt32.TryParse(str) with
      | true, value -> create value
      | false, _ -> Error PositiveIntError.CantParseStringToPositiveInt

    let one = { Value = 1u }

  type ItemWithSerial = { Name: ItemName; Serial: Serial }

  [<RequireQualifiedAccess>]
  module ItemWithSerial =

    let create name serial = { Name = name; Serial = serial }

  type ItemWithMacAddress =
    { Name: ItemName
      Serial: Serial
      MacAddress: MacAddress }

  [<RequireQualifiedAccess>]
  module ItemWithMacAddress =

    let create name serial macaddress =
      { Name = name
        Serial = serial
        MacAddress = macaddress }

  type ItemWithOnlyName = { Name: ItemName }

  [<RequireQualifiedAccess>]
  module ItemWithOnlyName =

    let create name = { Name = name }

  [<RequireQualifiedAccess>]
  type Item =
    | ItemWithSerial of ItemWithSerial
    | ItemWithMacAddress of ItemWithMacAddress
    | ItemWithOnlyName of ItemWithOnlyName

    member self.Name =
      match self with
      | ItemWithMacAddress i -> i.Name
      | ItemWithOnlyName i -> i.Name
      | ItemWithSerial i -> i.Name

    member self.MacAddress =
      match self with
      | ItemWithMacAddress i -> i.MacAddress |> Some
      | ItemWithOnlyName _ -> None
      | ItemWithSerial _ -> None

    member self.Serial =
      match self with
      | ItemWithMacAddress i -> i.Serial |> Some
      | ItemWithOnlyName _ -> None
      | ItemWithSerial i -> i.Serial |> Some

  [<RequireQualifiedAccess>]
  module Item =

    let createWithSerial name serial =
      ItemWithSerial.create name serial
      |> Item.ItemWithSerial

    let createWithMacAddress name serial macaddress =
      ItemWithMacAddress.create name serial macaddress
      |> Item.ItemWithMacAddress

    let createWithOnlyName name =
      ItemWithOnlyName.create name
      |> Item.ItemWithOnlyName

    let create name (serial: Serial option) (macaddress: MacAddress option) =
      if serial.IsSome && macaddress.IsSome then
        createWithMacAddress name serial.Value macaddress.Value
      elif serial.IsSome && macaddress.IsNone then
        createWithSerial name serial.Value
      else
        createWithOnlyName name

  [<NoEquality>]
  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type CacheCommand =
    | Initialization of Env
    | EmployerByChatId of ChatId * AsyncReplyChannel<RecordedEmployer option>
    | ManagerByChatId of ChatId * AsyncReplyChannel<RecordedManager option>
    //| OfficeByManagerChatId of ChatId * AsyncReplyChannel<RecordedOffice   option>
    | Offices of AsyncReplyChannel<RecordedOffice list option>
    | AddOffice of RecordedOffice
    | AddEmployer of RecordedEmployer
    | AddManager of RecordedManager
    | CurrentCache of AsyncReplyChannel<Cache>
    | GetOfficeEmployers of RecordedOffice * AsyncReplyChannel<RecordedEmployer list option>
    | DeleteOffice of RecordedOffice

  type Cache =
    { Employers: RecordedEmployer list
      Offices: RecordedOffice list
      Managers: RecordedManager list }

  [<NoEquality>]
  [<NoComparison>]
  type Logging =
    { Debug: string -> unit
      Info: string -> unit
      Error: string -> unit
      Warning: string -> unit
      Fatal: string -> unit }

  [<NoEquality>]
  [<NoComparison>]
  type Env =
    { Log: Logging
      Config: BotConfig
      DBConn: SqliteConnection
      CacheActor: MailboxProcessor<CacheCommand> }

  type RecordedManager =
    { ChatId: ChatId
      FirstName: FirstName
      LastName: LastName }

  type RecordedOffice =
    { OfficeName: OfficeName
      Manager: RecordedManager }

  type RecordedEmployer =
    { FirstName: FirstName
      LastName: LastName
      Office: RecordedOffice
      ChatId: ChatId }

  [<RequireQualifiedAccess>]
  module RecordedEmployer =

    let create firstName lastName office chatId =
      { FirstName = firstName
        LastName = lastName
        Office = office
        ChatId = chatId }

  type RecordedDeletionItem =
    { Item: Item
      Count: PositiveInt
      Time: DateTime
      Location: Location option
      Employer: RecordedEmployer }

    override self.ToString() =
      let macText =
        if self.Item.MacAddress.IsSome then
          %self.Item.MacAddress.Value
        else
          "Нет"

      let serialText =
        if self.Item.Serial.IsSome then
          %self.Item.Serial.Value
        else
          "Нет"

      let locationText =
        if self.Location.IsSome then
          %self.Location.Value
        else
          "Не указано"

      $"
        Имя позиции    : {self.Item.Name}
        Мак адрес      : {macText}
        Серийный номер : {serialText}
        Куда или зачем : {locationText}
        Количество     : {self.Count.GetValue}
        Дата           : {self.Time}"
