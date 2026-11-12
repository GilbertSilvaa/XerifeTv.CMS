using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.People.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.People;

public sealed class PersonRepository(IOptions<DBSettings> options, IMongoClient mongoClient)
  : BaseRepository<PersonEntity>(ECollection.PEOPLE, options, mongoClient), IPersonRepository
{
    public async Task<PersonEntity?> GetByIdAsync(string id)
        => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<PersonEntity?> GetByTmdbIdAsync(int tmdbId)
        => await _collection.Find(r => r.TmdbId == tmdbId).FirstOrDefaultAsync();

    public async Task<IReadOnlyCollection<PersonEntity>> GetByTmdbIdsAsync(IEnumerable<int> tmdbIds)
    {
        var ids = tmdbIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        var nullableIds = ids.Select(id => (int?)id).ToArray();
        return await _collection.Find(r => nullableIds.Contains(r.TmdbId)).ToListAsync();
    }

    public async Task<IReadOnlyCollection<PersonEntity>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        if (idList.Length == 0) return [];

        return await _collection.Find(r => idList.Contains(r.Id)).ToListAsync();
    }

    public async Task<IReadOnlyCollection<PersonEntity>> SearchByNameAsync(string name, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(name)) return [];

        var filter = Builders<PersonEntity>.Filter.Regex(
            r => r.Name,
            new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));

        return await _collection.Find(filter)
            .SortBy(r => r.Name)
            .Limit(limit)
            .ToListAsync();
    }
}
