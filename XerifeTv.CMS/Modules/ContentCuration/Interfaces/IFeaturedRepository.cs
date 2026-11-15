using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface IFeaturedRepository : IBaseRepository<FeaturedEntity>
{
    Task<IReadOnlyList<FeaturedEntity>> GetOrderedAsync(bool onlyActive);
    Task<FeaturedEntity?> GetHomeHeroAsync();
    Task UnsetHomeHeroAsync(string? exceptId = null);
}
