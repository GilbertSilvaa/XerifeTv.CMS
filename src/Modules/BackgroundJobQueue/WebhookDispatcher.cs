using System.Text;
using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public interface IWebhookDispatcher
{
    Task<bool> DispatchAsync(
        WebhookEntity webhook,
        EWebhookTriggerEvent triggerEvent,
        string entityId,
        string? payloadContent,
        CancellationToken cancellationToken);
}

public sealed class WebhookDispatcher(
    IWebhookDispatchHistoryService dispatchHistoryService,
    ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
{
    private const int MaxRetryAttempts = 5;

    public async Task<bool> DispatchAsync(
        WebhookEntity webhook,
        EWebhookTriggerEvent triggerEvent,
        string entityId,
        string? payloadContent,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(webhook.Url),
            Method = webhook.HttpMethod.ToHttpMethod()
        };

        foreach (var header in webhook.Headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (!string.IsNullOrWhiteSpace(payloadContent))
            request.Content = new StringContent(payloadContent, Encoding.UTF8, "application/json");

        var requestHeadersJson = JsonSerializer.Serialize(
            request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

        var historyResult = await dispatchHistoryService.StartAsync(
            webhook, triggerEvent, entityId, requestHeadersJson, payloadContent);

        var historyId = historyResult.IsSuccess ? historyResult.Data : null;

        if (historyResult.IsFailure)
            logger.LogWarning("Continuing dispatch without history tracking for webhook {WebhookId}", webhook.Id);

        WebhookDispatchAttemptResult? lastAttempt = null;
        var isSuccess = false;

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var attemptRequest = CloneRequest(request, payloadContent);
                var sendResult = await SendRequestAsync(httpClient, attemptRequest, webhook);
                lastAttempt = sendResult;

                if (historyId is not null)
                {
                    await dispatchHistoryService.RegisterAttemptAsync(historyId, new WebhookDispatchAttemptLog(
                        AttemptNumber: attempt,
                        AttemptedAt: DateTime.UtcNow,
                        Success: sendResult.IsSuccess,
                        StatusCode: sendResult.StatusCode,
                        ReasonPhrase: sendResult.ReasonPhrase,
                        ErrorMessage: sendResult.ErrorMessage,
                        ErrorType: sendResult.ErrorType));
                }

                if (sendResult.IsSuccess)
                {
                    isSuccess = true;
                    break;
                }

                logger.LogWarning(
                    "Retrying webhook {WebhookName}, attempt {Attempt}/{MaxAttempts}",
                    webhook.Name, attempt, MaxRetryAttempts);

                await Task.Delay(TimeSpan.FromSeconds(attempt * 6), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing webhook {WebhookName} on attempt {Attempt}", webhook.Name, attempt);

                lastAttempt = WebhookDispatchAttemptResult.FromException(ex);

                if (historyId is not null)
                {
                    await dispatchHistoryService.RegisterAttemptAsync(historyId, new WebhookDispatchAttemptLog(
                        AttemptNumber: attempt,
                        AttemptedAt: DateTime.UtcNow,
                        Success: false,
                        StatusCode: null,
                        ReasonPhrase: null,
                        ErrorMessage: ex.Message,
                        ErrorType: ex.GetType().Name));
                }

                if (attempt == MaxRetryAttempts) break;
            }
        }

        if (historyId is not null && lastAttempt is not null)
        {
            await dispatchHistoryService.FinishAsync(
                historyId, isSuccess, lastAttempt.StatusCode, lastAttempt.ResponseHeaders, lastAttempt.ResponseBody);
        }

        return isSuccess;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, string? payloadContent)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (!string.IsNullOrWhiteSpace(payloadContent))
            clone.Content = new StringContent(payloadContent, Encoding.UTF8, "application/json");

        return clone;
    }

    private async Task<WebhookDispatchAttemptResult> SendRequestAsync(
        HttpClient httpClient,
        HttpRequestMessage requestMessage,
        WebhookEntity webhook)
    {
        var response = await httpClient.SendAsync(requestMessage);
        var responseBody = response.Content != null ? await response.Content.ReadAsStringAsync() : null;
        var responseHeaders = JsonSerializer.Serialize(
            response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Webhook {WebhookName} failed with status {StatusCode}", webhook.Name, (int)response.StatusCode);

            return WebhookDispatchAttemptResult.FromFailure(
                (int)response.StatusCode, response.ReasonPhrase, responseHeaders, responseBody);
        }

        logger.LogInformation("Webhook {WebhookName} executed successfully with status code {StatusCode}", webhook.Name, (int)response.StatusCode);

        return WebhookDispatchAttemptResult.FromSuccess((int)response.StatusCode, responseHeaders, responseBody);
    }
}

public record WebhookDispatchAttemptResult(
    bool IsSuccess,
    int? StatusCode,
    string? ReasonPhrase,
    string? ResponseHeaders,
    string? ResponseBody,
    string? ErrorMessage,
    string? ErrorType)
{
    public static WebhookDispatchAttemptResult FromSuccess(int statusCode, string? headers, string? body)
        => new(true, statusCode, null, headers, body, null, null);

    public static WebhookDispatchAttemptResult FromFailure(int statusCode, string? reason, string? headers, string? body)
        => new(false, statusCode, reason, headers, body, null, null);

    public static WebhookDispatchAttemptResult FromException(Exception ex)
        => new(false, null, null, null, null, ex.Message, ex.GetType().Name);
}
