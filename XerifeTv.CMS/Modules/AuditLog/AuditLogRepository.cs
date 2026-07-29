using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.AuditLog;

public class AuditLogRepository(IOptions<DBSettings> dbSettings) : IAuditLogRepository
{
    protected readonly IMongoCollection<AuditLogEntity> _collection = new MongoClient(dbSettings.Value.ConnectionString)
        .GetDatabase(dbSettings.Value.DatabaseName)
        .GetCollection<AuditLogEntity>(ECollection.AUDIT_LOGS.ToString());

    public async Task AddAsync(AuditLogEntity entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public async Task<PagedList<AuditLogEntity>> GetAsync(int currentPage, int limit)
    {
        var count = await _collection.CountDocumentsAsync(_ => true);
        var items = await _collection.Find(_ => true)
          .SortByDescending(r => r.CreateAt)
          .Skip(limit * (currentPage - 1))
          .Limit(limit)
          .ToListAsync();

        var totalPages = (int)Math.Ceiling(count / (decimal)limit);

        return new PagedList<AuditLogEntity>(currentPage, totalPages, items);
    }

    public async Task<PagedList<AuditLogEntity>> GetAsync(string userId, int currentPage, int limit)
    {
        var count = await _collection.CountDocumentsAsync(d => d.UserId == userId);
        var items = await _collection.Find(d => d.UserId == userId)
          .SortByDescending(r => r.CreateAt)
          .Skip(limit * (currentPage - 1))
          .Limit(limit)
          .ToListAsync();

        var totalPages = (int)Math.Ceiling(count / (decimal)limit);

        return new PagedList<AuditLogEntity>(currentPage, totalPages, items);
    }
}
