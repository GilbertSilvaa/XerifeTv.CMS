using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Entities;

public sealed class FeaturedEntity : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsHomeHero { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxItems { get; set; } = CurationConstants.DefaultFeaturedMaxItems;
    public List<FeaturedItem> Items { get; set; } = [];
}

public sealed class FeaturedItem : BaseEntity
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
    public int Order { get; set; }
}
