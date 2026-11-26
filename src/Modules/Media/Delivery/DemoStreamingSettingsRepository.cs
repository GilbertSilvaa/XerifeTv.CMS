using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public sealed class DemoStreamingSettingsRepository(
    IOptions<DBSettings> dbSettings,
    IMongoClient mongoClient)
    : BaseRepository<DemoStreamingSettingsEntity>(
        ECollection.DEMO_STREAMING_SETTINGS, dbSettings, mongoClient),
      IDemoStreamingSettingsRepository
{
    public async Task<DemoStreamingSettingsEntity> GetSettingsAsync()
    {
        var settings = await _collection.Find(_ => true).FirstOrDefaultAsync();

        if (settings is not null)
            return settings;

        settings = new DemoStreamingSettingsEntity();
        await _collection.InsertOneAsync(settings);

        return settings;
    }

    public async Task CreateOrUpdateAsync(DemoStreamingSettingsEntity settings)
    {
        var register = await _collection.Find(r => r.Id == settings.Id).FirstOrDefaultAsync();

        if (register is null)
        {
            await _collection.InsertOneAsync(settings);
            return;
        }

        settings.UpdateAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(r => r.Id == settings.Id, settings);
    }
}
