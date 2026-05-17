using Microsoft.Extensions.Caching.Memory;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Abstractions.Services;

public sealed class CacheService(IMemoryCache _cache) : ICacheService
{
    private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
      .SetSlidingExpiration(TimeSpan.FromMinutes(10))
      .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
      .SetPriority(CacheItemPriority.Normal);

    public T? GetValue<T>(string key)
    {
        _cache.TryGetValue(key, out T? result);
        return result;
    }

    public void SetValue<T>(string key, T value)
      => _cache.Set(key, value, _cacheOptions);

    public void Remove(string key)
      => _cache.Remove(key);
}