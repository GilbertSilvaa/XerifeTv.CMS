using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Franchise;

public sealed class FranchiseRepository(IOptions<DBSettings> options)
    : BaseRepository<Franchise>(ECollection.FRANCHISES, options), IFranchiseRepository
{
    public async Task<IEnumerable<Franchise>> GetAllAsync()
    {
        return await _collection
            .Find(_ => true)
            .SortBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Franchise>> SearchByNameAsync(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        var normalizedSearch = search.Trim();
        if (normalizedSearch.Length < 2)
            return [];

        return await _collection
            .Find(r => r.Name.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase))
            .SortBy(r => r.Name)
            .Limit(20)
            .ToListAsync();
    }

    public async Task<Franchise?> GetByNameAsync(string name)
    {
        return await _collection
            .Find(r => r.Name.ToLower() == name.Trim().ToLower())
            .FirstOrDefaultAsync();
    }

    public async Task<Franchise?> GetByNameAsync(string name, string ignoreId)
    {
        return await _collection
            .Find(r => r.Name.ToLower() == name.Trim().ToLower() && r.Id != ignoreId)
            .FirstOrDefaultAsync();
    }
}
