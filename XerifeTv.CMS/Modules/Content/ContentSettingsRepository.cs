using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Content;

public class ContentSettingsRepository(IOptions<DBSettings> dbSettings)
    : BaseRepository<ContentSettingsEntity>(ECollection.CONTENT_API_SETTINGS, dbSettings), IContentSettingsRepository
{
    public async Task CreateOrUpdateAsync(ContentSettingsEntity contentSettings)
    {
        var register = await _collection.Find(r => r.Id == contentSettings.Id).FirstOrDefaultAsync();

        if (register == null)
        {
            await _collection.InsertOneAsync(contentSettings);
        }
        else
        {
            await _collection.ReplaceOneAsync(r => r.Id == contentSettings.Id, contentSettings);
        }
    }

    public async Task<ContentSettingsEntity?> GetContentSettingsAsync()
    {
        return await _collection.Find(_ => true).FirstOrDefaultAsync();
    }
}
