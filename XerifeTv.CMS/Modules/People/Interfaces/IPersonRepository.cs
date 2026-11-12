using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.People;

namespace XerifeTv.CMS.Modules.People.Interfaces;

public interface IPersonRepository : IBaseRepository<PersonEntity>
{
    Task<PersonEntity?> GetByIdAsync(string id);
    Task<PersonEntity?> GetByTmdbIdAsync(int tmdbId);
    Task<IReadOnlyCollection<PersonEntity>> GetByTmdbIdsAsync(IEnumerable<int> tmdbIds);
    Task<IReadOnlyCollection<PersonEntity>> GetByIdsAsync(IEnumerable<string> ids);
    Task<IReadOnlyCollection<PersonEntity>> SearchByNameAsync(string name, int limit = 20);
}
