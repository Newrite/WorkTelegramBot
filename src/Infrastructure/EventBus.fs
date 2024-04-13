namespace WorkTelegram.Infrastructure

open System.Collections.Generic
open WorkTelegram.Core
open FSharp.UMX

[<NoEquality>]
[<NoComparison>]
[<RequireQualifiedAccess>]
type EventBusMessage = | RemoveFromElmishDict of ChatId

type EventsStack = Stack<EventBusMessage>

[<Interface>]
type IEvent =
  abstract Events: EventsStack

[<Interface>]
type IEventBus =
  abstract Bus: IEvent

module EventBus =
  
  let removeFromDictEvent (env: #IEventBus) chatId =
    Logger.info env "Add event RemoveFromElmishDict for chat id %d" %chatId
    env.Bus.Events.Push(EventBusMessage.RemoveFromElmishDict chatId)

  let IEventBus eventStack =
    { new IEventBus with
        member _.Bus =
          { new IEvent with
              member _.Events = eventStack } }
