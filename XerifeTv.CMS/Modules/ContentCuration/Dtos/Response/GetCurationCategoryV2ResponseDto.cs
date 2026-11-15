using System.Text.Json.Serialization;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class GetCurationCategoryV2ResponseDto
{
    public string Category { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsPaginated { get; set; }

    public int CurrentPage { get; set; }
    public int TotalPageCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public List<object> Items { get; set; } = [];
}
