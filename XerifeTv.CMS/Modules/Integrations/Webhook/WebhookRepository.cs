using Microsoft.Extensions.Options;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Integrations.Webhook;

public sealed class WebhookRepository(IOptions<DBSettings> options)
    : BaseRepository<WebhookEntity>(ECollection.WEBHOOKS, options), IWebhookRepository
{ }
