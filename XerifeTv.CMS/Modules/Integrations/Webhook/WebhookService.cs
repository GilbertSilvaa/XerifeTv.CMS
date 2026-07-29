using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

namespace XerifeTv.CMS.Modules.Integrations.Webhook;

public sealed class WebhookService(IWebhookRepository repository) : IWebhookService
{
    public async Task<Result<PagedList<GetWebhookResponseDto>>> GetAsync(int currentPage, int limit)
    {
        try
        {
            var response = await repository.GetAsync(currentPage, limit);

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
            var response = await repository.CreateAsync(entity);

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
            var response = await repository.GetAsync(entity.Id);

            if (response is null)
                return Result<string>.Failure(new Error("404", "Webhook não encontrado"));

            entity.CreateAt = response.CreateAt;
            await repository.UpdateAsync(entity);
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
            var response = await repository.GetAsync(id);

            if (response is null)
                return Result<bool>.Failure(new Error("404", "Webhook não encontrado"));

            await repository.DeleteAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

    public async Task<Result<IEnumerable<WebhookEntity>>> GetByTriggerEventAsync(EWebhookTriggerEvent @event, bool includeDisabled = false)
    {
        try
        {
            var response = await repository.GetByTriggerEventAsync(@event, includeDisabled);
            return Result<IEnumerable<WebhookEntity>>.Success(response);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<IEnumerable<WebhookEntity>>.Failure(error);
        }
    }
}

