using XerifeTv.CMS.Modules.Abstractions.Entities;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public sealed class DemoStreamingSettingsEntity : BaseEntity
{
    public string VideoUrl { get; set; } = string.Empty;
}
