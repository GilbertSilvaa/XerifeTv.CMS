using XerifeTv.CMS.Models.Abstractions.Entities;

namespace XerifeTv.CMS.Models.Abstractions.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
  Task<PagedList<T>> GetAsync(int currentPage, int limit);
  Task<T?> GetAsync(string id);
  Task<string> CreateAsync(T entity);
  Task UpdateAsync(T entity);
  Task DeleteAsync(string id);
  Task<long> CountAsync();
}
