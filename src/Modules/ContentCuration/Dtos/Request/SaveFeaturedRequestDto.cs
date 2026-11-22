using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;

public sealed class SaveFeaturedRequestDto
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsHomeHero { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxItems { get; set; } = CurationConstants.DefaultFeaturedMaxItems;
    public List<CurationContentItemRequestDto> Items { get; set; } = [];
}

public sealed class CurationContentItemRequestDto
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
    public int Order { get; set; }
}
