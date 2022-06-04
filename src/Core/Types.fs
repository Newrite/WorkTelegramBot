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

  [<Measure>]
  type private officeid

  [<Measure>]
  type private deletionid

  type ItemName = string<itemname>
  type Serial = string<serialnumber>
  type MacAddress = string<macaddress>
  type LastName = string<lastname>
  type FirstName = string<firstname>
  type OfficeName = string<officename>
  type Location = string<location>
  type ChatId = int64<chatid>
  type OfficeId = int64<officeid>
  type DeletionId = int64<deletionid>

open UMX

[<AutoOpen>]
module rec Types =

  [<RequireQualifiedAccess>]
  type BusinessError =
    | NotFoundInDatabase
    | IncorrectMacAddress
    | NumberMostBePositive
    | IncorrectParseResult

  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type AppError =
    | DatabaseError of Donald.DbError
    | BusinessError of BusinessError

  [<RequireQualifiedAccess>]
  module MacAddress =

    let validate (input: string) =

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

      let r = Text.RegularExpressions.Regex("[a-fA-F0-9]{12}$")

      match r.IsMatch(inputCleaned) with
      | true ->
        let macAddress = format (inputCleaned.ToCharArray()) "" 0 0
        %macAddress |> Ok
      | false -> BusinessError.IncorrectMacAddress |> Error

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
        Error BusinessError.NumberMostBePositive

    let tryParse (str: string) =
      match UInt32.TryParse(str) with
      | true, value -> create value
      | false, _ -> Error BusinessError.IncorrectParseResult

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
    | EmployerByChatId of ChatId * AsyncReplyChannel<Employer option>
    | ManagerByChatId of ChatId * AsyncReplyChannel<Manager option>
    //| OfficeByManagerChatId of ChatId * AsyncReplyChannel<RecordedOffice   option>
    | Offices of AsyncReplyChannel<Office list option>
    | AddOffice of Office
    | AddEmployer of Employer
    | AddManager of Manager
    | CurrentCache of AsyncReplyChannel<Cache>
    | GetOfficeEmployers of Office * AsyncReplyChannel<Employer list option>
    | DeleteOffice of Office

  type Cache =
    { Employers: Employer list
      Offices: Office list
      Managers: Manager list }

  [<NoEquality>]
  [<NoComparison>]
  type Logging =
    { Debug: string -> unit
      Info: string -> unit
      Error: string -> unit
      Warning: string -> unit
      Fatal: string -> unit }

  type Manager =
    { ChatId: ChatId
      FirstName: FirstName
      LastName: LastName }

  type Office =
    { OfficeId: OfficeId
      IsHidden: bool
      OfficeName: OfficeName
      Manager: Manager }

  type Employer =
    { FirstName: FirstName
      LastName: LastName
      Office: Office
      ChatId: ChatId }

  [<RequireQualifiedAccess>]
  module Employer =

    let create firstName lastName office chatId =
      { FirstName = firstName
        LastName = lastName
        Office = office
        ChatId = chatId }

  type DeletionItem =
    { DeletionId: DeletionId
      Item: Item
      Count: PositiveInt
      Time: DateTime
      IsDeletion: bool
      IsHidden: bool
      Location: Location option
      Employer: Employer }

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
