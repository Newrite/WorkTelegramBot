namespace WorkTelegram.Infrastructure

open WorkTelegram.Core

type OfficesMap = Map<OfficeId, Office>
type EmployersMap = Map<ChatId, Employer>
type ManagersMap = Map<ChatId, Manager>
type DeletionItemsMap = Map<DeletionId, DeletionItem>
type MessagesMap = Map<ChatId, TelegramMessage>
