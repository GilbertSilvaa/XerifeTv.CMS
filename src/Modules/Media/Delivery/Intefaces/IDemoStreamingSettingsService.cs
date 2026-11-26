using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IDemoStreamingSettingsService
{
    Task<Result<GetDemoStreamingSettingsResponseDto>> GetAsync();
    Task<Result<string>> UpdateAsync(UpdateDemoStreamingSettingsRequestDto dto);
}
