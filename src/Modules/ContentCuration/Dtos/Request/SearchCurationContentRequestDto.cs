using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;

public sealed class SearchCurationContentRequestDto
{
    public string Term { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
    public int Limit { get; set; } = 20;
}
