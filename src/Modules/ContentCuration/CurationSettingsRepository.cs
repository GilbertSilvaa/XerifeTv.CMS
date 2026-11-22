using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationSettingsRepository(
    IOptions<DBSettings> dbSettings,
    IMongoClient mongoClient,
    ICacheService cacheService)
    : BaseRepository<CurationSettingsEntity>(ECollection.CURATION_SETTINGS, dbSettings, mongoClient), ICurationSettingsRepository
{
    private const string VersionCacheKey = "curation_settings_version";
    private static readonly TimeSpan VersionCacheTtl = TimeSpan.FromSeconds(5);

    public async Task<CurationSettingsEntity> GetSettingsAsync()
    {
        var settings = await _collection.Find(_ => true).FirstOrDefaultAsync();

        if (settings is not null)
            return settings;

        settings = new CurationSettingsEntity { Version = 0 };
        await _collection.InsertOneAsync(settings);

        return settings;
    }

    public async Task<long> GetVersionAsync()
    {
        var version = await cacheService.GetOrCreateAsync<long?>(
            VersionCacheKey,
            VersionCacheTtl,
            async () =>
            {
                var settings = await GetSettingsAsync();
                return settings.Version;
            });

        return version ?? 0;
    }

    public async Task IncrementVersionAsync()
    {
        var settings = await GetSettingsAsync();

        var update = Builders<CurationSettingsEntity>.Update
            .Inc(x => x.Version, 1)
            .Set(x => x.UpdateAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(x => x.Id == settings.Id, update);
        await cacheService.RemoveAsync(VersionCacheKey);
    }
}
