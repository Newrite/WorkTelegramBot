namespace WorkTelegram.Core

open Funogram
open Funogram.Types

[<AutoOpen>]
module Ext =

  let inline (^) f x = f x

module Funogram =

  module Telegram =

    module Types =

      /// This object represents a bot command.
      type BotCommand =
        { /// Text of the command; 1-32 characters. Can contain only lowercase English letters, digits and underscores.
          Command: string
          /// Description of the command; 1-256 characters.
          Description: string }

    module RequestsTypes =

      open Types

      type SetMyCommandsReq =
        { Commands: BotCommand array
          LanguageCode: string option }

        interface IRequestBase<bool> with
          member __.MethodName = "setMyCommands"

      type DeleteMyCommandsReq =
        { LanguageCode: string option }

        interface IRequestBase<bool> with
          member __.MethodName = "deleteMyCommands"

      type GetMyCommandsReq =
        { LanguageCode: string option }

        interface IRequestBase<BotCommand array> with
          member __.MethodName = "getMyCommands"

    module Api =

      open RequestsTypes

      let private setMyCommandsBase commands languageCode =
        { SetMyCommandsReq.Commands = commands
          SetMyCommandsReq.LanguageCode = languageCode }

      let setMyCommands commands = setMyCommandsBase commands None

      let setMyCommandsWithLanguageCode commands languageCode =
        setMyCommandsBase commands (Some languageCode)

      let private getMyCommandsBase languageCode = { GetMyCommandsReq.LanguageCode = languageCode }

      let getMyCommands () = getMyCommandsBase None

      let getMyCommandsWithLanguageCode languageCode = getMyCommandsBase (Some languageCode)

      let private deleteMyCommandsBase languageCode =
        { DeleteMyCommandsReq.LanguageCode = languageCode }

      let deleteMyCommands () = deleteMyCommandsBase None

      let deleteMyCommandsWithLanguageCode languageCode = deleteMyCommandsBase (Some languageCode)
