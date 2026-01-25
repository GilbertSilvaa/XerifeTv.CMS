using Amazon.Runtime.Internal;
using Microsoft.AspNetCore;
using System.Text;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Integrations.Webhook;

public sealed class WebhookService(
    IWebhookRepository _repository,
    IMovieRepository _movieRepository,
    ISeriesRepository _seriesRepository,
    IChannelRepository _channelRepository,
    ILogger<WebhookService> _logger) : IWebhookService
{
    public async Task<Result<PagedList<GetWebhookResponseDto>>> GetAsync(int currentPage, int limit)
    {
        try
        {
            var response = await _repository.GetAsync(currentPage, limit);

            var result = new PagedList<GetWebhookResponseDto>(
                response.CurrentPage,
                response.TotalPageCount,
                response.Items.Select(GetWebhookResponseDto.FromEntity));

            return Result<PagedList<GetWebhookResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetWebhookResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateWebhookRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var response = await _repository.CreateAsync(entity);

            return Result<string>.Success(response);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdateWebhookRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var response = await _repository.GetAsync(entity.Id);

            if (response is null)
                return Result<string>.Failure(new Error("404", "Webhook nao encontrado"));

            entity.CreateAt = response.CreateAt;
            await _repository.UpdateAsync(entity);
            return Result<string>.Success(entity.Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id)
    {
        try
        {
            var response = await _repository.GetAsync(id);

            if (response is null)
                return Result<bool>.Failure(new Error("404", "Webhook nao encontrado"));

            await _repository.DeleteAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

    public async Task DispacthWebhooksByTriggerEventAsync(EWebhookTriggerEvent @event, string idEntity)
    {
        const int MAX_RETRY_ATTEMPTS = 5;

        var webhooks = await _repository.GetByTriggerEventAsync(@event);

        using HttpClient httpClient = new();

        foreach (var webhook in webhooks)
        {
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

                    string? payloadContent = await BuildPayloadAsync(@event, idEntity, webhook);

                    if (!string.IsNullOrWhiteSpace(payloadContent))
                        request.Content = new StringContent(payloadContent, Encoding.UTF8, "application/json");

                    var result = await SendRequestWebhookAsync(httpClient, request, webhook);

                    if (result.IsSuccess) break;

                    _logger.LogWarning("Retrying webhook {WebhookName}, attempt {Attempt}/{MaxAttempts}", webhook.Name, attempt, MAX_RETRY_ATTEMPTS);

                    await Task.Delay(TimeSpan.FromSeconds(attempt * 6));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing webhook {WebhookName} on attempt {Attempt}", webhook.Name, attempt);

                    if (attempt == MAX_RETRY_ATTEMPTS) break;
                }
            }
        }
    }

    private async Task<string?> BuildPayloadAsync(EWebhookTriggerEvent @event, string idEntity, WebhookEntity webhook)
    {
        if (!webhook.HttpMethod.IsBodySupported() || string.IsNullOrWhiteSpace(webhook.PayloadTemplate))
            return null;

        return @event switch
        {
            EWebhookTriggerEvent.MOVIE_PUBLISHED =>
                ReplacePayload(await _movieRepository.GetAsync(idEntity)),

            EWebhookTriggerEvent.SERIES_PUBLISHED =>
                ReplacePayload(await _seriesRepository.GetAsync(idEntity)),

            EWebhookTriggerEvent.CHANNEL_PUBLISHED =>
                ReplacePayload(await _channelRepository.GetAsync(idEntity)),

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
        var response = await httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("The webhook {WebhookName} returned the status code {StatusCode}", webhook.Name, response.StatusCode);
            return Result<bool>.Failure(new Error(response.StatusCode.ToString()));
        }

        _logger.LogInformation("The webhook {WebhookName} was executed successfully", webhook.Name);
        return Result<bool>.Success(true);
    }
}