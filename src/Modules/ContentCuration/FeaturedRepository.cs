using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class FeaturedRepository(IOptions<DBSettings> dbSettings, IMongoClient mongoClient)
    : BaseRepository<FeaturedEntity>(ECollection.CURATION_FEATUREDS, dbSettings, mongoClient), IFeaturedRepository
{
    public async Task<IReadOnlyList<FeaturedEntity>> GetOrderedAsync(bool onlyActive)
    {
        var filter = onlyActive
            ? Builders<FeaturedEntity>.Filter.Eq(x => x.IsActive, true)
            : Builders<FeaturedEntity>.Filter.Empty;

        return await _collection
            .Find(filter)
            .SortBy(x => x.Order)
            .ThenBy(x => x.CreateAt)
            .ToListAsync();
    }

    public async Task<FeaturedEntity?> GetHomeHeroAsync()
    {
        return await _collection
            .Find(x => x.IsActive && x.IsHomeHero)
            .SortBy(x => x.Order)
            .FirstOrDefaultAsync();
    }

    public async Task UnsetHomeHeroAsync(string? exceptId = null)
    {
        var filter = Builders<FeaturedEntity>.Filter.Eq(x => x.IsHomeHero, true);

        if (!string.IsNullOrWhiteSpace(exceptId))
            filter &= Builders<FeaturedEntity>.Filter.Ne(x => x.Id, exceptId);

        var update = Builders<FeaturedEntity>.Update.Set(x => x.IsHomeHero, false);
        await _collection.UpdateManyAsync(filter, update);
    }
}
