namespace WorkTelegram.Infrastructure
    
    type OfficesMap = Map<Core.UMX.OfficeId,Core.Types.Office>
    
    type EmployersMap = Map<Core.UMX.ChatId,Core.Types.Employer>
    
    type ManagersMap = Map<Core.UMX.ChatId,Core.Types.Manager>
    
    type DeletionItemsMap = Map<Core.UMX.DeletionId,Core.Types.DeletionItem>
    
    type MessagesMap = Map<Core.UMX.ChatId,Core.Types.TelegramMessage>

