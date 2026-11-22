using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class GetCurationContentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public EContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public bool Disabled { get; set; }
}
