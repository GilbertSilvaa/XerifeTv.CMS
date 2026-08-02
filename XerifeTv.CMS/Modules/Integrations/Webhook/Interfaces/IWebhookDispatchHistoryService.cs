using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

public interface IWebhookDispatchHistoryService
{
    Task<Result<string>> StartAsync(
        WebhookEntity webhook,
        EWebhookTriggerEvent triggerEvent,
        string entityId,
        string? requestHeaders,
        string? requestBody);

    Task<Result<bool>> RegisterAttemptAsync(string historyId, WebhookDispatchAttemptLog attempt);

    Task<Result<bool>> FinishAsync(
        string historyId,
        bool success,
        int? statusCode,
        string? responseHeaders,
        string? responseBody);

    Task<Result<PagedList<GetWebhookDispatchHistoryResponseDto>>> GetHistoryAsync(
        string? webhookId,
        EWebhookTriggerEvent? triggerEvent,
        EWebhookDispatchStatus? status,
        int page = 1,
        int limit = 10);

    Task<Result<GetWebhookDispatchHistoryResponseDto>> RedispatchAsync(string historyId);
}