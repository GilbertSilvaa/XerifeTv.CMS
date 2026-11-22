using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public class MediaDeliveryProfileEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string StreamFormat { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParameters { get; set; } = [];
    public EMediaDeliveryTokenStrategyType TokenStrategy { get; set; } = EMediaDeliveryTokenStrategyType.NONE;
    public bool IsDisabled { get; set; } = false;
}