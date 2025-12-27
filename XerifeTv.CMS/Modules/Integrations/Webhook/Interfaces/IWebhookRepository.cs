using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

public interface IWebhookRepository : IBaseRepository<WebhookEntity>
{
    Task<IEnumerable<WebhookEntity>> GetByTriggerEventAsync(EWebhookTriggerEvent @event, bool includeDisabled = false);
}
