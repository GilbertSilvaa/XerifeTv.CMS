using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationCollectionRepository(IOptions<DBSettings> dbSettings, IMongoClient mongoClient)
    : BaseRepository<CurationCollectionEntity>(ECollection.CURATION_COLLECTIONS, dbSettings, mongoClient), ICurationCollectionRepository
{
    public async Task<IReadOnlyList<CurationCollectionEntity>> GetOrderedAsync(bool onlyActive)
    {
        var filter = onlyActive
            ? Builders<CurationCollectionEntity>.Filter.Eq(x => x.IsActive, true)
            : Builders<CurationCollectionEntity>.Filter.Empty;

        return await _collection
            .Find(filter)
            .SortBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<CurationCollectionEntity?> GetByNameAsync(string name)
    {
        var pattern = new BsonRegularExpression(
            $"^{Regex.Escape(name.Trim())}$", "i");

        return await _collection
            .Find(Builders<CurationCollectionEntity>.Filter.Regex(x => x.Name, pattern))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> NameExistsAsync(string name, string? ignoreId = null)
    {
        var pattern = new BsonRegularExpression(
            $"^{Regex.Escape(name.Trim())}$", "i");

        var filter = Builders<CurationCollectionEntity>.Filter.Regex(x => x.Name, pattern);

        if (!string.IsNullOrWhiteSpace(ignoreId))
            filter &= Builders<CurationCollectionEntity>.Filter.Ne(x => x.Id, ignoreId);

        return await _collection.Find(filter).AnyAsync();
    }
}
