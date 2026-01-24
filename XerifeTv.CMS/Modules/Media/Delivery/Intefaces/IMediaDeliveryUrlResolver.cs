using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IMediaDeliveryUrlResolver
{
    Task<Result<string>> ResolveUrlAsync(string mediaPath, string mediaDeliveryProfileId);
}