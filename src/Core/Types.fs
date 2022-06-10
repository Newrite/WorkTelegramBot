﻿namespace WorkTelegram.Core

open Donald
open FSharp.UMX
open System
open WorkTelegram.Core

[<AutoOpen>]
module UMX =
  [<Measure>]
  type private itemname

  [<Measure>]
  type private serialnumber

  //[<Measure>]
  //type private macaddress

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
  //type MacAddress = string<macaddress>
  type LastName = string<lastname>
  type FirstName = string<firstname>
  type OfficeName = string<officename>
  type Location = string<location>
  type ChatId = int64<chatid>
  type OfficeId = int64<officeid>
  type DeletionId = int64<deletionid>

open UMX

[<AutoOpen>]
module Types =

  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type BusinessError =
    | NotFoundInDatabase of SearchedType: Type
    | IncorrectMacAddress of PassedIncorrectValue: string
    | NumberMustBePositive of PassedIncorrectNumber: uint32
    | IncorrectParsePositiveNumber of PassedIncorrectStringToParse: string

  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type AppError =
    | DatabaseError of DbError
    | BusinessError of BusinessError

  module ErrorPatterns =

    [<AutoOpen>]
    module DatabasePatterns =

      let (|ErrDataReaderOutOfRangeError|_|) (error: AppError) =
        match error with
        | AppError.DatabaseError databaseError ->
          match databaseError with
          | DbError.DataReaderOutOfRangeError dataReaderOutOfRangeError ->
            Some(ErrDataReaderOutOfRangeError dataReaderOutOfRangeError)
          | _ -> None
        | _ -> None

      let (|ErrDataReaderCastError|_|) (error: AppError) =
        match error with
        | AppError.DatabaseError databaseError ->
          match databaseError with
          | DbError.DataReaderCastError dataReaderCastError ->
            Some(ErrDataReaderCastError dataReaderCastError)
          | _ -> None
        | _ -> None

      let (|ErrDbConnectionError|_|) (error: AppError) =
        match error with
        | AppError.DatabaseError databaseError ->
          match databaseError with
          | DbError.DbConnectionError dbConnectionError ->
            Some(ErrDbConnectionError dbConnectionError)
          | _ -> None
        | _ -> None

      let (|ErrDbTransactionError|_|) (error: AppError) =
        match error with
        | AppError.DatabaseError databaseError ->
          match databaseError with
          | DbError.DbTransactionError dbTransactionError ->
            Some(ErrDbTransactionError dbTransactionError)
          | _ -> None
        | _ -> None

      let (|ErrDbExecutionError|_|) (error: AppError) =
        match error with
        | AppError.DatabaseError databaseError ->
          match databaseError with
          | DbError.DbExecutionError dbExecutionError -> Some(ErrDbExecutionError dbExecutionError)
          | _ -> None
        | _ -> None

    [<AutoOpen>]
    module BusinessPatterns =

      let (|ErrNotFoundInDatabase|_|) (error: AppError) =
        match error with
        | AppError.BusinessError businessError ->
          match businessError with
          | BusinessError.NotFoundInDatabase searchedType ->
            Some(ErrNotFoundInDatabase searchedType)
          | _ -> None
        | _ -> None

      let (|ErrIncorrectMacAddress|_|) (error: AppError) =
        match error with
        | AppError.BusinessError businessError ->
          match businessError with
          | BusinessError.IncorrectMacAddress incorrectMac ->
            Some(ErrIncorrectMacAddress incorrectMac)
          | _ -> None
        | _ -> None

      let (|ErrNumberMustBePositive|_|) (error: AppError) =
        match error with
        | AppError.BusinessError businessError ->
          match businessError with
          | BusinessError.NumberMustBePositive incorrectNumber ->
            Some(ErrNumberMustBePositive incorrectNumber)
          | _ -> None
        | _ -> None

      let (|ErrIncorrectParsePositiveNumber|_|) (error: AppError) =
        match error with
        | AppError.BusinessError businessError ->
          match businessError with
          | BusinessError.IncorrectParsePositiveNumber incorrectString ->
            Some(ErrIncorrectParsePositiveNumber incorrectString)
          | _ -> None
        | _ -> None

  [<Struct>]
  type MacAddress =
    private
      { Value: string }

    member self.GetValue = self.Value

    override self.ToString() = self.Value

  [<RequireQualifiedAccess>]
  module MacAddress =

    let fromString (input: string) =

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
        { Value = macAddress } |> Ok
      | false ->
        input
        |> BusinessError.IncorrectMacAddress
        |> Error

    let fromOptionString (input: string option) =
      match input with
      | Some m -> fromString m |> Option.ofResult
      | None -> None

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
        count
        |> BusinessError.NumberMustBePositive
        |> Error

    let tryParse (str: string) =
      match UInt32.TryParse(str) with
      | true, value -> create value
      | false, _ ->
        str
        |> BusinessError.IncorrectParsePositiveNumber
        |> Error

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

  [<RequireQualifiedAccess>]
  module Manager =

    let asEmployer (manager: Manager) office =
      { FirstName = manager.FirstName
        LastName = manager.LastName
        Office = office
        ChatId = manager.ChatId }

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
          self.Item.MacAddress.Value.Value
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
