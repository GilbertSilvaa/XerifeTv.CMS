using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook;

public sealed class WebhookEntity : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Url { get; set; } = default!;
    public EHttpMethod HttpMethod { get; set; } = EHttpMethod.POST;
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? PayloadTemplate { get; set; }
    public EWebhookTriggerEvent TriggerEvent { get; set; }
    public bool IsDisabled { get; set; } = false;
}