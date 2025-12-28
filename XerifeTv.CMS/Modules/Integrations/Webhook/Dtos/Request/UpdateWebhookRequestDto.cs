using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;

public class UpdateWebhookRequestDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public EHttpMethod HttpMethod { get; init; } = EHttpMethod.POST;
    public List<KeyValueInput> Headers { get; init; } = [];
    public string? PayloadTemplate { get; init; }
    public EWebhookTriggerEvent TriggerEvent { get; init; }
    public bool IsDisabled { get; init; }

    public WebhookEntity ToEntity()
    {
        return new WebhookEntity
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Url = Url,
            HttpMethod = HttpMethod,
            Headers = Headers.Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToDictionary(x => x.Key!, x => x.Value ?? ""),
            PayloadTemplate = PayloadTemplate,
            TriggerEvent = TriggerEvent,
            IsDisabled = IsDisabled
        };
    }
}
