using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public class MediaDeliveryProfileRepository : BaseRepository<MediaDeliveryProfileEntity>, IMediaDeliveryProfileRepository
{
    public MediaDeliveryProfileRepository(IOptions<DBSettings> dbSettings) : base(ECollection.MEDIA_DELIVERY_PROFILES, dbSettings) { }

    public async Task<IEnumerable<MediaDeliveryProfileEntity>> GetAsync(bool isIncludeDisabled = false)
    {
        return await _collection.Find(r => (isIncludeDisabled) || (!isIncludeDisabled && !r.IsDisabled))
            .ToListAsync();
    }

    public async Task<MediaDeliveryProfileEntity?> GetByNameAsync(string name, bool isIncludeDisabled = false)
    {
        return await _collection.Find(r
            => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
               ((isIncludeDisabled) || (!isIncludeDisabled && !r.IsDisabled)))
            .FirstOrDefaultAsync();
    }
}
