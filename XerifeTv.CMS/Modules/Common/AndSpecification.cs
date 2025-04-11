using XerifeTv.CMS.Modules.User;

namespace XerifeTv.CMS.Modules.Common;

public sealed class AndSpecification<T>(
  ISpecification<T> _left, 
  ISpecification<T> _right) : ISpecification<T> where T : class
{
  public async Task<bool> IsSatisfiedByAsync(T entity)
    => await _left.IsSatisfiedByAsync(entity) && await _right.IsSatisfiedByAsync(entity);
}