namespace XerifeTv.CMS.Modules.Common;

public interface ISpecification<T> where T : class
{
  Task<bool> IsSatisfiedByAsync(T entity);
}