namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class GetHomeContentV2ResponseDto
{
    public List<FeaturedContent> FeaturedContents = [];
    public string[] MovieCategores { get; set; } = [];
    public string[] SeriesCategores { get; set; } = [];
}

public record FeaturedContent(object? Content, string Type);