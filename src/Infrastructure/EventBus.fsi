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
        
        val IEventBus: eventStack: EventsStack -> IEventBus

