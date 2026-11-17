using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Models;

public sealed record CurationSnapshotSlice(
    IReadOnlyList<CurationSnapshotItem> Items,
    int TotalCount);
