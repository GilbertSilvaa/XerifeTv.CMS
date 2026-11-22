using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Models;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationSnapshotService
{
    Task<CurationSnapshotSlice?> GetSliceAsync(
        CurationCollectionEntity collection, int page, int pageSize);

    Task<IReadOnlyDictionary<string, CurationSnapshotSlice>> GetFirstSlicesAsync(
        IReadOnlyList<CurationCollectionEntity> collections);

    Task RebuildAsync(
        CurationCollectionEntity collection, CancellationToken cancellationToken = default);

    Task<int> RebuildAllAsync(CancellationToken cancellationToken = default);

    Task RemoveAsync(string collectionId);
}
