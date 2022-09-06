namespace WorkTelegram.Telegram

open WorkTelegram.Core
open WorkTelegram.Infrastructure

open FSharp.UMX
open Funogram.Telegram.Types

module Utils =

  let sendMessageMarkup env (chatId: UMX.ChatId) text markup =
    Funogram.Telegram.Api.sendMessageMarkup %chatId text markup
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok message ->
        Logger.debug env $"Success send message markup with id = {message.MessageId} and markup "
        Ok message
      | Error error ->
        Logger.error env $"Error when try send message markup, error = {error} and markup"
        Error error

  let sendMessage env (chatId: UMX.ChatId) text =
    Funogram.Telegram.Api.sendMessage %chatId text
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok message ->
        Logger.debug env $"Success send message with id = {message.MessageId}"
        Ok message
      | Error error ->
        Logger.error env $"Error when try send message, error = {error}"
        Error error

  let editMessageTextBase env (message: TelegramMessage) text =
    Funogram.Telegram.Req.EditMessageText.Make(text, Int message.Chat.Id, message.MessageId)
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok editedMessage ->
        Logger.debug
          env
          $"Success edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"

        Ok editedMessage
      | Error error ->
        Logger.error

          env
          $"Can't edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"

        Error error

  let editMessageTextBaseMarkup env text (message: TelegramMessage) markup =
    Funogram.Telegram.Req.EditMessageText.Make(text, Int message.Chat.Id, message.MessageId, replyMarkup = markup)
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok editedMessage ->
        Logger.debug
          env
          $"Success edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"

        Ok editedMessage
      | Error error ->
        Logger.error
          env
          $"Can't edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"

        Error error


  let deleteMessageBase env (message: TelegramMessage) =
    Funogram.Telegram.Api.deleteMessage message.Chat.Id message.MessageId
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok editedMessage ->
        Logger.debug
          env
          $"Success delete message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"

        Ok editedMessage
      | Error error ->
        Logger.error
          env
          $"Can't delete message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"

        Error error

  let sendMessageAndDeleteAfterDelay env (chatId: UMX.ChatId) text (delay: int) =
    match sendMessage env chatId text with
    | Ok message ->
      task {
        do! Async.Sleep(delay)
        deleteMessageBase env message |> ignore
      }
      |> ignore
    | Error _ -> ()

  let sendDocument env (chatId: UMX.ChatId) (fileName: string) (fileStream: System.IO.Stream) =
    let fileToSend = InputFile.File(fileName, fileStream)

    Funogram.Telegram.Req.SendDocument.Make(%chatId, fileToSend)
    |> Funogram.Api.api (Configurer.botConfig env)
    |> Async.RunSynchronously
    |> function
      | Ok message ->
        Logger.debug env $"Success send document with id = {message.MessageId}"
        Ok message
      | Error error ->
        Logger.error env $"Error when try send document, error = {error}"
        Error error

  let sendDocumentAndDeleteAfterDelay
    env
    (chatId: UMX.ChatId)
    (fileName: string)
    (fileStream: System.IO.Stream)
    (delay: int)
    =
    match sendDocument env chatId fileName fileStream with
    | Ok message ->
      task {
        do! Async.Sleep(delay)
        deleteMessageBase env message |> ignore
      }
      |> ignore
    | Error _ -> ()
