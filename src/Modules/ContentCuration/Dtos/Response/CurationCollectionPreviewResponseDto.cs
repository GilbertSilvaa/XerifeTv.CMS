using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class CurationCollectionPreviewResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Limit { get; set; }
    public List<CurationPreviewItemResponseDto> Items { get; set; } = [];
}

public sealed class CurationPreviewItemResponseDto
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public DateTime CreateAt { get; set; }
}
