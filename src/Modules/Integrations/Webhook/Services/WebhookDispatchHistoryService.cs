using System.Text;
using System.Text.Json;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Services;

public class WebhookDispatchHistoryService(
    IWebhookDispatchHistoryRepository repository,
    IWebhookRepository webhookRepository) : IWebhookDispatchHistoryService
{
    public async Task<Result<string>> StartAsync(
        WebhookEntity webhook,
        EWebhookTriggerEvent triggerEvent,
        string entityId,
        string? requestHeaders,
        string? requestBody)
    {
        try
        {
            var entity = WebhookDispatchHistoryEntity.Create(
                webhookId: webhook.Id,
                webhookName: webhook.Name,
                url: webhook.Url,
                httpMethod: webhook.HttpMethod,
                triggerEvent: triggerEvent,
                entityId: entityId,
                requestHeaders: requestHeaders,
                requestBody: requestBody);

            var result = await repository.CreateAsync(entity);

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<bool>> RegisterAttemptAsync(string historyId, WebhookDispatchAttemptLog attempt)
    {
        try
        {
            var history = await repository.GetAsync(historyId);

            if (history is null)
                return Result<bool>.Failure(new Error("404", "Webhook dispatch history not found"));

            history.RegisterAttempt(attempt);

            await repository.UpdateAsync(history);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

    public async Task<Result<bool>> FinishAsync(
        string historyId,
        bool success,
        int? statusCode,
        string? responseHeaders,
        string? responseBody)
    {
        try
        {
            var history = await repository.GetAsync(historyId);

            if (history is null)
                return Result<bool>.Failure(new Error("404", "Webhook dispatch history not found"));

            if (success)
                history.MarkAsSuccess(statusCode!.Value, responseHeaders, responseBody);
            else
                history.MarkAsFailed(statusCode, responseHeaders, responseBody);

            await repository.UpdateAsync(history);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

    public async Task<Result<PagedList<GetWebhookDispatchHistoryResponseDto>>> GetHistoryAsync(
        string? webhookId,
        EWebhookTriggerEvent? triggerEvent,
        EWebhookDispatchStatus? status,
        int page = 1,
        int limit = 10)
    {
        try
        {
            var pagedEntities = await repository.GetByFilterAsync(webhookId, triggerEvent, status, page, limit);

            var items = pagedEntities.Items.Select(GetWebhookDispatchHistoryResponseDto.FromEntity);
            var result = new PagedList<GetWebhookDispatchHistoryResponseDto>(
                pagedEntities.CurrentPage,
                pagedEntities.TotalPageCount,
                items);

            return Result<PagedList<GetWebhookDispatchHistoryResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetWebhookDispatchHistoryResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<GetWebhookDispatchHistoryResponseDto>> RedispatchAsync(string historyId)
    {
        try
        {
            var originalHistory = await repository.GetAsync(historyId);
            if (originalHistory is null)
                return Result<GetWebhookDispatchHistoryResponseDto>.Failure(new Error("404", "Histórico de disparo não encontrado"));

            var webhook = await webhookRepository.GetAsync(originalHistory.WebhookId);

            var webhookName = webhook?.Name ?? originalHistory.WebhookName;
            var targetUrl = webhook?.Url ?? originalHistory.Url;
            var httpMethod = webhook?.HttpMethod ?? originalHistory.HttpMethod;
            var requestHeadersStr = originalHistory.RequestHeaders;
            var requestBodyStr = originalHistory.RequestBody;

            var newHistoryEntity = WebhookDispatchHistoryEntity.Create(
                webhookId: originalHistory.WebhookId,
                webhookName: webhookName,
                url: targetUrl,
                httpMethod: httpMethod,
                triggerEvent: originalHistory.TriggerEvent,
                entityId: originalHistory.EntityId,
                requestHeaders: requestHeadersStr,
                requestBody: requestBodyStr);

            var newHistoryId = await repository.CreateAsync(newHistoryEntity);

            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(httpMethod.ToHttpMethod(), new Uri(targetUrl));

            if (webhook?.Headers != null && webhook.Headers.Count > 0)
            {
                foreach (var header in webhook.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestBodyStr) && httpMethod.IsBodySupported())
            {
                request.Content = new StringContent(requestBodyStr, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage? response = null;
            int? statusCode = null;
            string? responseHeaders = null;
            string? responseBody = null;
            bool isSuccess = false;
            string? errorMessage = null;

            try
            {
                var attemptAt = DateTime.UtcNow;
                response = await httpClient.SendAsync(request);
                statusCode = (int)response.StatusCode;
                isSuccess = response.IsSuccessStatusCode;

                if (response.Content != null)
                    responseBody = await response.Content.ReadAsStringAsync();

                responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

                var attemptLog = new WebhookDispatchAttemptLog(
                    AttemptNumber: 1,
                    AttemptedAt: attemptAt,
                    Success: isSuccess,
                    StatusCode: statusCode,
                    ReasonPhrase: response.ReasonPhrase,
                    ErrorMessage: isSuccess ? null : response.ReasonPhrase,
                    ErrorType: isSuccess ? null : "HttpRequestException");

                newHistoryEntity.RegisterAttempt(attemptLog);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                isSuccess = false;
                var attemptLog = new WebhookDispatchAttemptLog(
                    AttemptNumber: 1,
                    AttemptedAt: DateTime.UtcNow,
                    Success: false,
                    StatusCode: null,
                    ReasonPhrase: null,
                    ErrorMessage: ex.Message,
                    ErrorType: ex.GetType().Name);

                newHistoryEntity.RegisterAttempt(attemptLog);
            }

            if (isSuccess)
                newHistoryEntity.MarkAsSuccess(statusCode ?? 200, responseHeaders, responseBody);
            else
                newHistoryEntity.MarkAsFailed(statusCode, responseHeaders, responseBody ?? errorMessage);

            await repository.UpdateAsync(newHistoryEntity);

            return Result<GetWebhookDispatchHistoryResponseDto>.Success(GetWebhookDispatchHistoryResponseDto.FromEntity(newHistoryEntity));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetWebhookDispatchHistoryResponseDto>.Failure(error);
        }
    }
}