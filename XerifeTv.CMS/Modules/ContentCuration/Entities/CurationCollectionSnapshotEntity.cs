using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Entities;

public sealed class CurationCollectionSnapshotEntity : BaseEntity
{
    public string CollectionId { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public bool AllowsPagination { get; set; }
    public int PageSize { get; set; } = CurationConstants.DefaultCollectionLimit;
    public int TotalCount { get; set; }
    public long CurationVersion { get; set; }
    public List<CurationSnapshotItem> Items { get; set; } = [];
}

public sealed class CurationSnapshotItem
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
}
