namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T?>> factory);
    Task<T?> GetValueAsync<T>(string key);
    Task SetValueAsync<T>(string key, TimeSpan ttl, T value);
    Task RemoveAsync(string key);
}