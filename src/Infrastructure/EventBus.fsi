namespace WorkTelegram.Infrastructure
    
    [<NoEquality; NoComparison; RequireQualifiedAccess>]
    type EventBusMessage =
        | RemoveFromElmishDict of WorkTelegram.Core.UMX.ChatId
    
    type EventsStack = System.Collections.Generic.Stack<EventBusMessage>
    
    type IEvent =
        
        abstract Events: EventsStack
    
    type IEventBus =
        
        abstract Bus: IEvent
    
    module EventBus =
        
        val removeFromDictEvent:
          env: 'a -> chatId: WorkTelegram.Core.UMX.ChatId -> unit
            when 'a :> IEventBus and 'a :> ILog
        
        val IEventBus: eventStack: EventsStack -> IEventBus

