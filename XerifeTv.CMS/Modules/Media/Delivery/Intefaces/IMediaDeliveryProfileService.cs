using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IMediaDeliveryProfileService
{
    Task<Result<PagedList<GetMediaDeliveryProfileResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<IEnumerable<GetMediaDeliveryProfileResponseDto>>> GetAllAsync(bool isIncludeDisabled = false);
    Task<Result<GetMediaDeliveryProfileResponseDto?>> GetAsync(string id);
    Task<Result<string>> CreateAsync(CreateMediaDeliveryProfileRequestDto dto);
    Task<Result<string>> UpdateAsync(UpdateMediaDeliveryProfileRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
}
