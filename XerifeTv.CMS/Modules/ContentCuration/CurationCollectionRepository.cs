using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationCollectionRepository(IOptions<DBSettings> dbSettings, IMongoClient mongoClient)
    : BaseRepository<CurationCollectionEntity>(ECollection.CURATION_COLLECTIONS, dbSettings, mongoClient), ICurationCollectionRepository
{
    private static readonly Collation NameCollation =
        new("en", strength: CollationStrength.Secondary);

    private static readonly FindOptions NameFindOptions = new()
    {
        Collation = NameCollation
    };

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
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _collection
            .Find(
                Builders<CurationCollectionEntity>.Filter.Eq(x => x.Name, name.Trim()),
                NameFindOptions)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<CurationCollectionEntity>> GetByNamesAsync(
        IEnumerable<string> names)
    {
        var nameList = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (nameList.Count == 0) return [];

        var filter = Builders<CurationCollectionEntity>.Filter.And(
            Builders<CurationCollectionEntity>.Filter.In(x => x.Name, nameList),
            Builders<CurationCollectionEntity>.Filter.Eq(x => x.IsActive, true));

        return await _collection
            .Find(filter, NameFindOptions)
            .ToListAsync();
    }

    public async Task<bool> NameExistsAsync(string name, string? ignoreId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var filter = Builders<CurationCollectionEntity>.Filter.Eq(x => x.Name, name.Trim());

        if (!string.IsNullOrWhiteSpace(ignoreId))
            filter &= Builders<CurationCollectionEntity>.Filter.Ne(x => x.Id, ignoreId);

        return await _collection
            .Find(filter, NameFindOptions)
            .AnyAsync();
    }
}
