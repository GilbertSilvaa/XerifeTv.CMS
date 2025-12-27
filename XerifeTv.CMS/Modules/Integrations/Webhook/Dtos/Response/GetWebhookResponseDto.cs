using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;

public class GetWebhookResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public EHttpMethod HttpMethod { get; private set; } = EHttpMethod.POST;
    public Dictionary<string, string> Headers { get; private set; } = [];
    public string? PayloadTemplate { get; private set; }
    public EWebhookTriggerEvent TriggerEvent { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    public bool IsDisabled { get; private set; }

    public static GetWebhookResponseDto FromEntity(WebhookEntity entity)
    {
        return new GetWebhookResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Url = entity.Url,
            HttpMethod = entity.HttpMethod,
            Headers = entity.Headers,
            PayloadTemplate = entity.PayloadTemplate,
            TriggerEvent = entity.TriggerEvent,
            RegistrationDate = entity.CreateAt,
            IsDisabled = entity.IsDisabled
        };
    }
}
