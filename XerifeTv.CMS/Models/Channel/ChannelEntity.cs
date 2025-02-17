using XerifeTv.CMS.Models.Abstractions.Entities;
using XerifeTv.CMS.Models.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Models.Channel;

public sealed class ChannelEntity : BaseEntity
{
  public string Title { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string LogoUrl { get; set; } = string.Empty;
  public Video? Video { get; set; }
  public bool? Disabled { get; set; } = false;
}
