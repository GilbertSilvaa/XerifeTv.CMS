using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

public interface IWebhookService
{
    Task<Result<PagedList<GetWebhookResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<string>> CreateAsync(CreateWebhookRequestDto dto);
    Task<Result<string>> UpdateAsync(UpdateWebhookRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
}
