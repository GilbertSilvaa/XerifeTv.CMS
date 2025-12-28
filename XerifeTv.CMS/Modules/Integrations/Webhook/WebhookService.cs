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
        var webhooks = await _repository.GetByTriggerEventAsync(@event);

        foreach (var webhook in webhooks)
        {
            try
            {
                using HttpClient httpClient = new();

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(webhook.Url),
                    Method = webhook.HttpMethod switch
                    {
                        EHttpMethod.POST => HttpMethod.Post,
                        EHttpMethod.PUT => HttpMethod.Put,
                        EHttpMethod.GET => HttpMethod.Get,
                        EHttpMethod.DELETE => HttpMethod.Delete,
                        _ => HttpMethod.Get
                    }
                };

                foreach (var header in webhook.Headers)
                    request.Headers.Add(header.Key, header.Value);

                if (webhook.HttpMethod != EHttpMethod.GET &&
                    webhook.HttpMethod != EHttpMethod.DELETE &&
                    !string.IsNullOrWhiteSpace(webhook.PayloadTemplate))
                {
                    string payloadContent = webhook.PayloadTemplate!;

                    if (@event == EWebhookTriggerEvent.MOVIE_PUBLISHED)
                    {
                        var movieEntity = await _movieRepository.GetAsync(idEntity);
                        if (movieEntity is null) continue;
                        payloadContent = @event.ReplaceKeywords(payloadContent, movieEntity);
                    }

                    if (@event == EWebhookTriggerEvent.SERIES_PUBLISHED)
                    {
                        var seriesEntity = await _seriesRepository.GetAsync(idEntity);
                        if (seriesEntity is null) continue;
                        payloadContent = @event.ReplaceKeywords(payloadContent, seriesEntity);
                    }

                    if (@event == EWebhookTriggerEvent.CHANNEL_PUBLISHED)
                    {
                        var channelEntity = await _channelRepository.GetAsync(idEntity);
                        if (channelEntity is null) continue;
                        payloadContent = @event.ReplaceKeywords(payloadContent, channelEntity);
                    }

                    request.Content = new StringContent(payloadContent, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("The webhook {WebhookName} returned the status code {StatusCode}", webhook.Name, response.StatusCode);
                    continue;
                }

                _logger.LogInformation("The webhook {WebhookName} was executed successfully", webhook.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("The following error occurred while trying to execute the webhook {WebhookName}: {ErrorMessage}", webhook.Name, ex.Message);
            }
        }
    }
}