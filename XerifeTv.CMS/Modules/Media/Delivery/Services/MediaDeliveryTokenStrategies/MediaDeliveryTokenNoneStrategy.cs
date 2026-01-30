using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Media.Delivery.Services.MediaTokenStrategies;

public class MediaDeliveryTokenNoneStrategy : IMediaDeliveryTokenStrategy
{
    public bool CanHandle(EMediaDeliveryTokenStrategyType tokenStrategyType)
        => tokenStrategyType == EMediaDeliveryTokenStrategyType.NONE;

    public Result<string> Resolve(Dictionary<string, string> @params)
    {
        return Result<string>.Success(string.Empty);
    }
}