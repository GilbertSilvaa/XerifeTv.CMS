using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;

public class CreateWebhookRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public EHttpMethod HttpMethod { get; init; } = EHttpMethod.POST;
    public List<KeyValueInput> Headers { get; init; } = [];
    public string? PayloadTemplate { get; init; }
    public EWebhookTriggerEvent TriggerEvent { get; init; }

    public WebhookEntity ToEntity()
    {
        return new WebhookEntity
        {
            Name = Name,
            Url = Url,
            Description = Description,
            HttpMethod = HttpMethod,
            Headers = Headers.Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToDictionary(x => x.Key!, x => x.Value ?? ""),
            PayloadTemplate = PayloadTemplate,
            TriggerEvent = TriggerEvent
        };
    }
}