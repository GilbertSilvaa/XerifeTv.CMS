using XerifeTv.CMS.Modules.Franchise;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Franchise.Interfaces;

public interface IFranchiseRepository : IBaseRepository<Franchise>
{
    Task<IEnumerable<Franchise>> GetAllAsync();
    Task<IEnumerable<Franchise>> SearchByNameAsync(string search);
    Task<Franchise?> GetByNameAsync(string name);
    Task<Franchise?> GetByNameAsync(string name, string ignoreId);
}
