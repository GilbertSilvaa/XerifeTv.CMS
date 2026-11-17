using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationCollectionSnapshotRepository : IBaseRepository<CurationCollectionSnapshotEntity>
{
    Task<CurationCollectionSnapshotEntity?> GetByCollectionIdAsync(string collectionId);
    Task<CurationCollectionSnapshotEntity?> GetPageAsync(string collectionId, int skip, int take);
    Task<IReadOnlyList<CurationCollectionSnapshotEntity>> GetFirstPageByCollectionIdsAsync(
        IEnumerable<string> collectionIds);
    Task<IReadOnlyList<string>> GetAllCollectionIdsAsync();
    Task UpsertAsync(CurationCollectionSnapshotEntity snapshot);
    Task DeleteByCollectionIdAsync(string collectionId);
    Task DeleteManyByCollectionIdsAsync(IEnumerable<string> collectionIds);
}
