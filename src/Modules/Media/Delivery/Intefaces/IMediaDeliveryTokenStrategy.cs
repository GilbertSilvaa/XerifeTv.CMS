using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IMediaDeliveryTokenStrategy
{
    Result<string> Resolve(Dictionary<string, string> @params);
    bool CanHandle(EMediaDeliveryTokenStrategyType tokenStrategyType);
}