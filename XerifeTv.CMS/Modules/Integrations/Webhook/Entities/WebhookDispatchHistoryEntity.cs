using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Entities;

public class WebhookDispatchHistoryEntity : BaseEntity
{
    public string WebhookId { get; private set; } = default!;
    public string WebhookName { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public EHttpMethod HttpMethod { get; private set; }
    public EWebhookTriggerEvent TriggerEvent { get; private set; }
    public string EntityId { get; private set; } = default!;

    public string? RequestHeaders { get; private set; }
    public string? RequestBody { get; private set; }

    public int? ResponseStatusCode { get; private set; }
    public string? ResponseHeaders { get; private set; }
    public string? ResponseBody { get; private set; }

    public EWebhookDispatchStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string AttemptsLog { get; private set; } = "[]";

    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private WebhookDispatchHistoryEntity() { }

    public static WebhookDispatchHistoryEntity Create(
        string webhookId,
        string webhookName,
        string url,
        EHttpMethod httpMethod,
        EWebhookTriggerEvent triggerEvent,
        string entityId,
        string? requestHeaders,
        string? requestBody)
    {
        return new WebhookDispatchHistoryEntity
        {
            WebhookId = webhookId,
            WebhookName = webhookName,
            Url = url,
            HttpMethod = httpMethod,
            TriggerEvent = triggerEvent,
            EntityId = entityId,
            RequestHeaders = requestHeaders,
            RequestBody = requestBody,
            Status = EWebhookDispatchStatus.PROCESSING,
            StartedAt = DateTime.UtcNow
        };
    }

    public void RegisterAttempt(WebhookDispatchAttemptLog attempt)
    {
        var attempts = DeserializeAttempts();
        attempts.Add(attempt);
        AttemptsLog = JsonSerializer.Serialize(attempts);
        AttemptCount = attempts.Count;
    }

    public void MarkAsSuccess(int statusCode, string? responseHeaders, string? responseBody)
    {
        ResponseStatusCode = statusCode;
        ResponseHeaders = responseHeaders;
        ResponseBody = responseBody;
        Status = EWebhookDispatchStatus.SUCCESS;
        FinishedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(int? statusCode, string? responseHeaders, string? responseBody)
    {
        ResponseStatusCode = statusCode;
        ResponseHeaders = responseHeaders;
        ResponseBody = responseBody;
        Status = EWebhookDispatchStatus.FAILED;
        FinishedAt = DateTime.UtcNow;
    }

    private List<WebhookDispatchAttemptLog> DeserializeAttempts()
        => string.IsNullOrWhiteSpace(AttemptsLog)
            ? []
            : JsonSerializer.Deserialize<List<WebhookDispatchAttemptLog>>(AttemptsLog) ?? [];
}

public record WebhookDispatchAttemptLog(
    int AttemptNumber,
    DateTime AttemptedAt,
    bool Success,
    int? StatusCode,
    string? ReasonPhrase,
    string? ErrorMessage,
    string? ErrorType);

public enum EWebhookDispatchStatus
{
    PROCESSING = 1,
    SUCCESS = 2,
    FAILED = 3
}