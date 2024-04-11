namespace WorkTelegram.Infrastructure
    
    type OfficesMap =
        Map<WorkTelegram.Core.UMX.OfficeId,WorkTelegram.Core.Types.Office>
    
    type EmployersMap =
        Map<WorkTelegram.Core.UMX.ChatId,WorkTelegram.Core.Types.Employer>
    
    type ManagersMap =
        Map<WorkTelegram.Core.UMX.ChatId,WorkTelegram.Core.Types.Manager>
    
    type DeletionItemsMap =
        Map<WorkTelegram.Core.UMX.DeletionId,
            WorkTelegram.Core.Types.DeletionItem>
    
    type MessagesMap =
        Map<WorkTelegram.Core.UMX.ChatId,WorkTelegram.Core.Types.TelegramMessage>

