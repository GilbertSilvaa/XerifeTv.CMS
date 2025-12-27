using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Integrations.Webhook;

public sealed class WebhookRepository(IOptions<DBSettings> options)
    : BaseRepository<WebhookEntity>(ECollection.WEBHOOKS, options), IWebhookRepository
{
    public async Task<IEnumerable<WebhookEntity>> GetByTriggerEventAsync(EWebhookTriggerEvent @event, bool includeDisabled = false)
    {
        return await _collection
            .Find(r => r.TriggerEvent == @event && (includeDisabled || !r.IsDisabled))
            .ToListAsync();
    }
}
