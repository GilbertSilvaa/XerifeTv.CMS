using Microsoft.Extensions.Caching.Memory;
using XerifeTv.CMS.Models.Abstractions.Interfaces;

namespace XerifeTv.CMS.Models.Abstractions.Services;

public sealed class CacheService(IMemoryCache _cache) : ICacheService
{
  private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
    .SetSlidingExpiration(TimeSpan.FromSeconds(300))
    .SetAbsoluteExpiration(TimeSpan.FromSeconds(900))
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