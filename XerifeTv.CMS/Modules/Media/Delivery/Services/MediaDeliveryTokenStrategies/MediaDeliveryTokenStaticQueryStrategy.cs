using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Media.Delivery.Services.MediaTokenStrategies;

public class MediaDeliveryTokenStaticQueryStrategy : IMediaDeliveryTokenStrategy
{
    public bool CanHandle(EMediaDeliveryTokenStrategyType tokenStrategyType)
        => tokenStrategyType == EMediaDeliveryTokenStrategyType.STATIC_QUERY_PARAMETERS;

    public Result<string> Resolve(Dictionary<string, string> @params)
    {
        try
        {
            var queryParameters = string.Join("&", @params.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return Result<string>.Success(queryParameters);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }
}
