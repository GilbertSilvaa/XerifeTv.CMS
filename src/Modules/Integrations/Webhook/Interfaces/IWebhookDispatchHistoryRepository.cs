using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

public interface IWebhookDispatchHistoryRepository : IBaseRepository<WebhookDispatchHistoryEntity>
{
    Task<IEnumerable<WebhookDispatchHistoryEntity>> GetByWebhookIdAsync(string webhookId, int page, int pageSize);
    Task<IEnumerable<WebhookDispatchHistoryEntity>> GetByEntityIdAsync(string entityId, EWebhookTriggerEvent triggerEvent);
    Task<PagedList<WebhookDispatchHistoryEntity>> GetByFilterAsync(string? webhookId, EWebhookTriggerEvent? triggerEvent, EWebhookDispatchStatus? status, int page, int pageSize);
}