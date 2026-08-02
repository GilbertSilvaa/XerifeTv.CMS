using System.Text;
using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public class DispacthWebhooksBackgroundProcessorStrategy(
    IServiceProvider serviceProvider,
    ILogger<DispacthWebhooksBackgroundProcessorStrategy> logger) : IBackgroundJobProcessorStrategy
{
    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job)
    {
        const int MAX_RETRY_ATTEMPTS = 5;

        using var scope = serviceProvider.CreateScope();
        var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        var dispatchHistoryService = scope.ServiceProvider.GetRequiredService<IWebhookDispatchHistoryService>();

        var webhookTriggerEvent = job.Type switch
        {
            EBackgroundJobType.DISPATCH_WEBHOOKS_MOVIES => EWebhookTriggerEvent.MOVIE_PUBLISHED,
            EBackgroundJobType.DISPATCH_WEBHOOKS_SERIES => EWebhookTriggerEvent.SERIES_PUBLISHED,
            EBackgroundJobType.DISPATCH_WEBHOOKS_CHANNELS => EWebhookTriggerEvent.CHANNEL_PUBLISHED,
            _ => throw new InvalidOperationException($"Unsupported job type: {job.Type}")
        };

        var webhooksResult = await webhookService.GetByTriggerEventAsync(webhookTriggerEvent);

        if (webhooksResult.IsFailure)
        {
            logger.LogError("Failed to retrieve webhooks for event {Event}: {Error}", webhookTriggerEvent, webhooksResult.Error);
            return;
        }

        var webhooks = webhooksResult.Data!;

        var updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
        {
            Id = job.Id,
            TotalRecordsToProcess = webhooks.Count(),
            TotalSuccessfulRecords = 0,
            TotalProcessedRecords = 0,
            Status = EBackgroundJobStatus.PROCESSING
        };

        await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);

        Type repositoryType = job.Type switch
        {
            EBackgroundJobType.DISPATCH_WEBHOOKS_MOVIES => typeof(IMovieRepository),
            EBackgroundJobType.DISPATCH_WEBHOOKS_SERIES => typeof(ISeriesRepository),
            EBackgroundJobType.DISPATCH_WEBHOOKS_CHANNELS => typeof(IChannelRepository),
            _ => throw new InvalidOperationException($"Unsupported job type: {job.Type}")
        };

        dynamic repository = scope.ServiceProvider.GetRequiredService(repositoryType);

        using HttpClient httpClient = new();

        foreach (var webhook in webhooks)
        {
            bool isSuccess = false;

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(webhook.Url),
                Method = webhook.HttpMethod.ToHttpMethod()
            };

            foreach (var header in webhook.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            string? payloadContent = await BuildPayloadAsync(webhookTriggerEvent, job.DispatchWebhooksEntityId!, webhook, repository);

            if (!string.IsNullOrWhiteSpace(payloadContent))
                request.Content = new StringContent(payloadContent, Encoding.UTF8, "application/json");

            var requestHeadersJson = JsonSerializer.Serialize(
                request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

            var historyResult = await dispatchHistoryService.StartAsync(
                webhook, webhookTriggerEvent, job.DispatchWebhooksEntityId!, requestHeadersJson, payloadContent);

            if (historyResult.IsFailure)
                logger.LogWarning("Continuing dispatch without history tracking for webhook {WebhookId}", webhook.Id);

            var historyId = historyResult.IsSuccess ? historyResult.Data : null;

            WebhookDispatchAttemptResult? lastAttempt = null;

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    var attemptRequest = CloneRequest(request, payloadContent);

                    var sendResult = await SendRequestWebhookAsync(httpClient, attemptRequest, webhook);
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

                    logger.LogWarning("Retrying webhook {WebhookName}, attempt {Attempt}/{MaxAttempts}", webhook.Name, attempt, MAX_RETRY_ATTEMPTS);

                    await Task.Delay(TimeSpan.FromSeconds(attempt * 6));
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

                    if (attempt == MAX_RETRY_ATTEMPTS) break;
                }
            }

            if (historyId is not null && lastAttempt is not null)
            {
                await dispatchHistoryService.FinishAsync(
                    historyId, isSuccess, lastAttempt.StatusCode, lastAttempt.ResponseHeaders, lastAttempt.ResponseBody);
            }

            if (isSuccess)
            {
                updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
                {
                    Id = job.Id,
                    TotalRecordsToProcess = updateBackgroundJobDto.TotalRecordsToProcess,
                    TotalSuccessfulRecords = updateBackgroundJobDto.TotalSuccessfulRecords + 1,
                    TotalProcessedRecords = updateBackgroundJobDto.TotalProcessedRecords + 1,
                    Status = EBackgroundJobStatus.PROCESSING
                };
            }
            else
            {
                updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
                {
                    Id = job.Id,
                    TotalRecordsToProcess = updateBackgroundJobDto.TotalRecordsToProcess,
                    TotalSuccessfulRecords = updateBackgroundJobDto.TotalSuccessfulRecords,
                    TotalProcessedRecords = updateBackgroundJobDto.TotalProcessedRecords + 1,
                    TotalFailedRecords = updateBackgroundJobDto.TotalFailedRecords + 1,
                    ErrorList = [.. updateBackgroundJobDto.ErrorList, webhook.Id],
                    Status = EBackgroundJobStatus.PROCESSING
                };
            }

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }

        if (updateBackgroundJobDto.TotalProcessedRecords == updateBackgroundJobDto.TotalRecordsToProcess)
        {
            updateBackgroundJobDto.Status = updateBackgroundJobDto.TotalFailedRecords > 0
                ? EBackgroundJobStatus.FAILED
                : EBackgroundJobStatus.COMPLETED;

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType is EBackgroundJobType.DISPATCH_WEBHOOKS_MOVIES or
           EBackgroundJobType.DISPATCH_WEBHOOKS_SERIES or
           EBackgroundJobType.DISPATCH_WEBHOOKS_CHANNELS;

    private static async Task<string?> BuildPayloadAsync<T>(
        EWebhookTriggerEvent @event,
        string idEntity,
        WebhookEntity webhook,
        IBaseRepository<T> repository) where T : BaseEntity
    {
        if (!webhook.HttpMethod.IsBodySupported() || string.IsNullOrWhiteSpace(webhook.PayloadTemplate))
            return null;

        return @event switch
        {
            EWebhookTriggerEvent.MOVIE_PUBLISHED =>
                ReplacePayload(await repository.GetAsync(idEntity)),

            EWebhookTriggerEvent.SERIES_PUBLISHED =>
                ReplacePayload(await repository.GetAsync(idEntity)),

            EWebhookTriggerEvent.CHANNEL_PUBLISHED =>
                ReplacePayload(await repository.GetAsync(idEntity)),

            _ => null
        };

        string? ReplacePayload(BaseEntity? entity)
        {
            if (entity is null) return null;
            return @event.ReplaceKeywords(webhook.PayloadTemplate!, entity);
        }
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

    private async Task<WebhookDispatchAttemptResult> SendRequestWebhookAsync(
        HttpClient httpClient,
        HttpRequestMessage requestMessage,
        WebhookEntity webhook)
    {
        var response = await httpClient.SendAsync(requestMessage);
        var responseBody = response.Content != null ? await response.Content.ReadAsStringAsync() : null;
        var responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

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