namespace WorkTelegram.Telegram

open WorkTelegram.Core

open FSharp.UMX
open Funogram.Telegram.Types 

module Utils =

  let sendMessageMarkup (env: Env) (chatId: UMX.ChatId) text markup =
    Funogram.Telegram.Api.sendMessageMarkup %chatId text markup
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok message ->
      env.Log.Debug $"Success send message markup with id = {message.MessageId} and markup "
      Ok message
    | Error error ->
      env.Log.Error $"Error when try send message markup, error = {error} and markup"
      Error error

  let sendMessage (env: Env) (chatId: UMX.ChatId) text =
    Funogram.Telegram.Api.sendMessage %chatId text
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok message ->
      env.Log.Debug $"Success send message with id = {message.MessageId}"
      Ok message
    | Error error ->
      env.Log.Error $"Error when try send message, error = {error}"
      Error error

  let editMessageTextBase (env: Env) message text =
    Funogram.Telegram.Api.editMessageTextBase
      (Some (Int message.Chat.Id)) (Some message.MessageId) None text None None None
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok editedMessage ->
      env.Log.Debug $"Success edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"
      Ok editedMessage
    | Error error ->
      env.Log.Error $"Can't edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"
      Error error

  let editMessageTextBaseMarkup (env: Env) text message markup =
    Funogram.Telegram.Api.editMessageTextBase
      (Some (Int message.Chat.Id)) (Some message.MessageId) None text None None (Some markup)
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok editedMessage ->
      env.Log.Debug $"Success edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"
      Ok editedMessage
    | Error error ->
      env.Log.Error $"Can't edit message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"
      Error error

   
  let deleteMessageBase (env: Env) message =
    Funogram.Telegram.Api.deleteMessage
      message.Chat.Id message.MessageId
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok editedMessage ->
      env.Log.Debug $"Success delete message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}"
      Ok editedMessage
    | Error error ->
      env.Log.Error $"Can't delete message with id = {message.MessageId}
        in chat with id = {message.Chat.Id}, error = {error}"
      Error error
    
  let sendMessageAndDeleteAfterDelay (env: Env) (chatId: UMX.ChatId) text (delay: int) =
    match sendMessage env chatId text with
    | Ok message -> 
     task {
       do! Async.Sleep(delay)
       deleteMessageBase env message |> ignore
     } |> ignore
    | Error _ -> ()    

  let sendDocument (env: Env) (chatId: UMX.ChatId) (fileName: string) (fileStream: System.IO.Stream) =
    let fileToSend =
      FileToSend.File(fileName, fileStream)
    Funogram.Telegram.Api.sendDocumentBase (Int %chatId) fileToSend None None None None None None
    |> Funogram.Api.api env.Config
    |> Async.RunSynchronously
    |> function
    | Ok message ->
      fileStream.Dispose()
      env.Log.Debug $"Success send document with id = {message.MessageId}"
      Ok message
    | Error error ->
      fileStream.Dispose()
      env.Log.Error $"Error when try send document, error = {error}"
      Error error

  let sendDocumentAndDeleteAfterDelay (env: Env) (chatId: UMX.ChatId) (fileName: string) (fileStream: System.IO.Stream) (delay: int) =
    match sendDocument env chatId fileName fileStream with
    | Ok message -> 
     task {
       do! Async.Sleep(delay)
       deleteMessageBase env message |> ignore
     } |> ignore
    | Error _ -> ()   