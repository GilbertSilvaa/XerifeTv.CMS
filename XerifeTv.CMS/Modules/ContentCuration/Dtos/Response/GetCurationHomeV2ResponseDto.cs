using XerifeTv.CMS.Modules.Content.Dtos.Response;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class GetCurationHomeV2ResponseDto
{
    public List<FeaturedContent> Featureds { get; set; } = [];
    public string[] Categories { get; set; } = [];
}
