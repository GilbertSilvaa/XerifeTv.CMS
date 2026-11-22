using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Channel;

public sealed class ChannelEntity : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public ICollection<string> Categories { get; set; } = [];
    public string LogoUrl { get; set; } = string.Empty;
    public Video? Video { get; set; }
    public string? MediaDeliveryProfileId { get; set; }
    public string? MediaRoute { get; set; }
    public bool Disabled { get; set; } = false;
}
