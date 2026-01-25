using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IMediaDeliveryUrlResolver
{
    Task<Result<GetResolveUrlResponseDto>> ResolveUrlAsync(string mediaPath, string mediaDeliveryProfileId);
}