using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;

public class GetWebhookDispatchHistoryFilterRequestDto
{
    public string? WebhookId { get; set; }
    public EWebhookTriggerEvent? TriggerEvent { get; set; }
    public EWebhookDispatchStatus? Status { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int Limit { get; set; } = 10;
}
