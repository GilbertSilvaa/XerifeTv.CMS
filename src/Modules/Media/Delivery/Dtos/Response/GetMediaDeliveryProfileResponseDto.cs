using XerifeTv.CMS.Modules.Media.Delivery.Enums;

namespace XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

public class GetMediaDeliveryProfileResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public string StreamFormat { get; private set; } = string.Empty;
    public Dictionary<string, string> QueryParameters { get; private set; } = [];
    public EMediaDeliveryTokenStrategyType TokenStrategy { get; private set; }
    public bool IsDisabled { get; private set; } = false;

    public static GetMediaDeliveryProfileResponseDto FromEntity(MediaDeliveryProfileEntity entity)
    {
        return new()
        {
            Id = entity.Id,
            Name = entity.Name,
            BaseUrl = entity.BaseUrl,
            StreamFormat = entity.StreamFormat,
            QueryParameters = entity.QueryParameters,
            TokenStrategy = entity.TokenStrategy,
            IsDisabled = entity.IsDisabled
        };
    }
}