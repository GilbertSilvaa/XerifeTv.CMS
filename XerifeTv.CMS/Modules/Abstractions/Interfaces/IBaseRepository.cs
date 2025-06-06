using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<PagedList<T>> GetAsync(int currentPage, int limit);
    Task<T?> GetAsync(string id);
    Task<string> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
    Task<long> CountAsync();
}
