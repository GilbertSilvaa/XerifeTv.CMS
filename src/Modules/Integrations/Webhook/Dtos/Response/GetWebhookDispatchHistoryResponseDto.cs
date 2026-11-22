using System.Text.Json;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;

public class GetWebhookDispatchHistoryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public string WebhookName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public EHttpMethod HttpMethod { get; set; }
    public EWebhookTriggerEvent TriggerEvent { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string? RequestHeaders { get; set; }
    public string? RequestBody { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public EWebhookDispatchStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public List<WebhookDispatchAttemptLog> AttemptsLog { get; set; } = [];
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public static GetWebhookDispatchHistoryResponseDto FromEntity(WebhookDispatchHistoryEntity entity)
    {
        List<WebhookDispatchAttemptLog> attempts = [];
        if (!string.IsNullOrWhiteSpace(entity.AttemptsLog))
        {
            try
            {
                attempts = JsonSerializer.Deserialize<List<WebhookDispatchAttemptLog>>(entity.AttemptsLog) ?? [];
            }
            catch
            {
                attempts = [];
            }
        }

        return new GetWebhookDispatchHistoryResponseDto
        {
            Id = entity.Id,
            WebhookId = entity.WebhookId,
            WebhookName = entity.WebhookName,
            Url = entity.Url,
            HttpMethod = entity.HttpMethod,
            TriggerEvent = entity.TriggerEvent,
            EntityId = entity.EntityId,
            RequestHeaders = entity.RequestHeaders,
            RequestBody = entity.RequestBody,
            ResponseStatusCode = entity.ResponseStatusCode,
            ResponseHeaders = entity.ResponseHeaders,
            ResponseBody = entity.ResponseBody,
            Status = entity.Status,
            AttemptCount = entity.AttemptCount,
            AttemptsLog = attempts,
            StartedAt = entity.StartedAt,
            FinishedAt = entity.FinishedAt
        };
    }
}
