using System.Text;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public class DispacthWebhooksBackgroundProcessorStrategy : IBackgroundJobProcessorStrategy
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DispacthWebhooksBackgroundProcessorStrategy> _logger;

    public DispacthWebhooksBackgroundProcessorStrategy(
        IServiceProvider serviceProvider,
        ILogger<DispacthWebhooksBackgroundProcessorStrategy> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job)
    {
        const int MAX_RETRY_ATTEMPTS = 5;

        using var scope = _serviceProvider.CreateScope();
        var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

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
            _logger.LogError("Failed to retrieve webhooks for event {Event}: {Error}", webhookTriggerEvent, webhooksResult.Error);
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

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
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

                    var result = await SendRequestWebhookAsync(httpClient, request, webhook);

                    if (result.IsSuccess)
                    {
                        isSuccess = true;
                        break;
                    }

                    _logger.LogWarning("Retrying webhook {WebhookName}, attempt {Attempt}/{MaxAttempts}", webhook.Name, attempt, MAX_RETRY_ATTEMPTS);

                    await Task.Delay(TimeSpan.FromSeconds(attempt * 6));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing webhook {WebhookName} on attempt {Attempt}", webhook.Name, attempt);

                    if (attempt == MAX_RETRY_ATTEMPTS) break;
                }
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

    private async Task<string?> BuildPayloadAsync<T>(
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

    private async Task<Result<bool>> SendRequestWebhookAsync(
        HttpClient httpClient,
        HttpRequestMessage requestMessage,
        WebhookEntity webhook)
    {
        HttpResponseMessage? response = null;
        string? responseBody = null;

        try
        {
            response = await httpClient.SendAsync(requestMessage);

            responseBody = response.Content != null
                ? await response.Content.ReadAsStringAsync()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    """
                    Webhook execution failed
                    Webhook: {WebhookName}
                    Request:
                      Method: {Method}
                      Url: {Url}
                      Headers: {@RequestHeaders}

                    Response:
                      StatusCode: {StatusCode}
                      ReasonPhrase: {ReasonPhrase}
                      Headers: {@ResponseHeaders}
                      Body: {ResponseBody}
                    """,
                    webhook.Name,
                    requestMessage.Method.Method,
                    requestMessage.RequestUri,
                    requestMessage.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    responseBody
                );

                return Result<bool>.Failure(new Error(response.StatusCode.ToString(), "Webhook returned a non-success status code"));
            }

            _logger.LogInformation("Webhook {WebhookName} executed successfully with status code {StatusCode}", webhook.Name, (int)response.StatusCode);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                """
                Exception while executing webhook
                Webhook: {WebhookName}
                Request:
                  Method: {Method}
                  Url: {Url}
                  Headers: {@RequestHeaders}
                ResponseBody (if any): {ResponseBody}
                """,
                webhook.Name,
                requestMessage.Method.Method,
                requestMessage.RequestUri,
                requestMessage.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                responseBody
            );

            return Result<bool>.Failure(new Error("500", ex.Message));
        }
    }
}