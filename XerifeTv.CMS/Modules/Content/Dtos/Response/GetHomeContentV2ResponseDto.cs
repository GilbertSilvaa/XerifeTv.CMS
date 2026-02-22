namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class GetHomeContentV2ResponseDto
{
    public object? FeaturedContent { get; set; }
    public EFeaturedContentType FeaturedContentType { get; set; }
    public string[] MovieCategores { get; set; } = [];
    public string[] SeriesCategores { get; set; } = [];
}

public enum EFeaturedContentType
{
    MOVIE,
    SERIES
}