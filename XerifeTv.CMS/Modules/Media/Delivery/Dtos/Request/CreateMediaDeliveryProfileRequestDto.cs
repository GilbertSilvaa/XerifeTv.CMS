using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;

namespace XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;

public class CreateMediaDeliveryProfileRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string StreamFormat { get; init; } = string.Empty;
    public List<KeyValueInput> QueryParameters { get; init; } = [];
    public EMediaDeliveryTokenStrategyType TokenStrategy { get; init; } = EMediaDeliveryTokenStrategyType.NONE;

    public MediaDeliveryProfileEntity ToEntity()
    {
        return new MediaDeliveryProfileEntity
        {
            Name = Name,
            BaseUrl = BaseUrl,
            StreamFormat = StreamFormat,
            QueryParameters = QueryParameters.Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToDictionary(x => x.Key!, x => x.Value ?? ""),
            TokenStrategy = TokenStrategy
        };
    }
}
