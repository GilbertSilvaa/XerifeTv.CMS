using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationCollectionRepository : IBaseRepository<CurationCollectionEntity>
{
    Task<IReadOnlyList<CurationCollectionEntity>> GetOrderedAsync(bool onlyActive);
    Task<CurationCollectionEntity?> GetByNameAsync(string name);
    Task<bool> NameExistsAsync(string name, string? ignoreId = null);
}
