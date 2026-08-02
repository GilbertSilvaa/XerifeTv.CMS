using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Repositories;

public class WebhookDispatchHistoryRepository(IOptions<DBSettings> options, IMongoClient mongoClient)
    : BaseRepository<WebhookDispatchHistoryEntity>(ECollection.WEBHOOKS_HISTORY, options, mongoClient), IWebhookDispatchHistoryRepository
{
    public async Task<IEnumerable<WebhookDispatchHistoryEntity>> GetByEntityIdAsync(string entityId, EWebhookTriggerEvent triggerEvent)
    {
        return await _collection
            .Find(r => r.EntityId == entityId && r.TriggerEvent == triggerEvent)
            .ToListAsync();
    }

    public async Task<IEnumerable<WebhookDispatchHistoryEntity>> GetByWebhookIdAsync(string webhookId, int page, int pageSize)
    {
        return await _collection
            .Find(r => r.WebhookId == webhookId)
            .SortByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<PagedList<WebhookDispatchHistoryEntity>> GetByFilterAsync(
        string? webhookId,
        EWebhookTriggerEvent? triggerEvent,
        EWebhookDispatchStatus? status,
        int page,
        int pageSize)
    {
        var builder = Builders<WebhookDispatchHistoryEntity>.Filter;
        var filterDefinitions = new List<FilterDefinition<WebhookDispatchHistoryEntity>>();

        if (!string.IsNullOrWhiteSpace(webhookId))
            filterDefinitions.Add(builder.Eq(r => r.WebhookId, webhookId));

        if (triggerEvent.HasValue)
            filterDefinitions.Add(builder.Eq(r => r.TriggerEvent, triggerEvent.Value));

        if (status.HasValue)
            filterDefinitions.Add(builder.Eq(r => r.Status, status.Value));

        var combinedFilter = filterDefinitions.Count > 0 ? builder.And(filterDefinitions) : builder.Empty;

        var count = await _collection.CountDocumentsAsync(combinedFilter);
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(count / (decimal)pageSize) : 0;

        var items = await _collection
            .Find(combinedFilter)
            .SortByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PagedList<WebhookDispatchHistoryEntity>(page, totalPages, items);
    }
}
