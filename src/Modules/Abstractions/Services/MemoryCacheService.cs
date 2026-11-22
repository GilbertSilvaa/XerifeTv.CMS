using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Abstractions.Services;

public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _inFlight = new();

    public async Task<T?> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T?>> factory)
    {
        var result = await GetValueAsync<T>(key);
        if (result is not null) return result;

        var lazyTask = _inFlight.GetOrAdd(
            key,
            _ => new Lazy<Task<string?>>(async () =>
            {
                try
                {
                    var value = await factory();
                    if (value is null) return null;

                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = ttl
                    };

                    string serializedValue = JsonSerializer.Serialize(value);
                    cache.Set(key, serializedValue, cacheOptions);

                    return serializedValue;
                }
                finally
                {
                    _inFlight.TryRemove(key, out var _);
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var lazyResult = await lazyTask.Value;
            return lazyResult is null ? default : JsonSerializer.Deserialize<T>(lazyResult);
        }
        catch
        {
            _inFlight.TryRemove(key, out var _);
            throw;
        }
    }

    public async Task<T?> GetValueAsync<T>(string key)
    {
        if (cache.TryGetValue(key, out string? value))
        {
            return value is null ? default : JsonSerializer.Deserialize<T>(value);
        }

        return default;
    }

    public async Task RemoveAsync(string key)
    {
        _inFlight.TryRemove(key, out var _);
        cache.Remove(key);
    }

    public async Task SetValueAsync<T>(string key, TimeSpan ttl, T value)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        string serializedValue = JsonSerializer.Serialize(value);
        cache.Set(key, serializedValue, cacheOptions);
    }
}
