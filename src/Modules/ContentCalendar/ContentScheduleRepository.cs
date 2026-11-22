using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCalendar;

public sealed class ContentScheduleRepository(IOptions<DBSettings> options, IMongoClient mongoClient)
    : BaseRepository<ContentScheduleEntity>(ECollection.CONTENT_SCHEDULES, options, mongoClient),
      IContentScheduleRepository
{
    public async Task<IReadOnlyCollection<ContentScheduleEntity>> GetByPeriodAsync(DateOnly start, DateOnly end)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Gte(r => r.ScheduledDate, start),
            Builders<ContentScheduleEntity>.Filter.Lte(r => r.ScheduledDate, end));

        return await _collection.Find(filter)
            .SortBy(r => r.ScheduledDate)
            .ThenBy(r => r.Title)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ContentScheduleEntity>> GetDueUnnotifiedAsync(DateOnly today, int limit)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Ne(r => r.Status, EScheduleStatus.REGISTERED),
            Builders<ContentScheduleEntity>.Filter.Lte(r => r.ScheduledDate, today),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.NotificationProcessedAt, (DateTime?)null));

        return await _collection.Find(filter)
            .SortBy(r => r.ScheduledDate)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ContentScheduleEntity>> GetPendingAsync(DateOnly today, int limit)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Ne(r => r.Status, EScheduleStatus.REGISTERED),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.RegisteredContentId, (string?)null),
            Builders<ContentScheduleEntity>.Filter.Lte(r => r.ScheduledDate, today));

        return await _collection.Find(filter)
            .SortBy(r => r.ScheduledDate)
            .ThenBy(r => r.Title)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<ContentScheduleEntity?> GetByExternalKeyAsync(
        EContentScheduleType contentType, int tmdbId, int? seasonNumber, int? episodeNumber)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ContentType, contentType),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.TmdbId, tmdbId),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.SeasonNumber, seasonNumber),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.EpisodeNumber, episodeNumber));

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<ContentScheduleEntity>> GetByImdbIdAsync(
        string imdbId, EContentScheduleType contentType)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ImdbId, imdbId),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ContentType, contentType));

        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<ContentScheduleEntity?> GetEpisodeByImdbIdAsync(
        string imdbId, int seasonNumber, int episodeNumber)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ContentType, EContentScheduleType.EPISODE),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ImdbId, imdbId),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.SeasonNumber, seasonNumber),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.EpisodeNumber, episodeNumber));

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<ContentScheduleEntity>> GetEpisodesByImdbIdAndSeasonAsync(
        string imdbId, int seasonNumber)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ContentType, EContentScheduleType.EPISODE),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ImdbId, imdbId),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.SeasonNumber, seasonNumber));

        return await _collection.Find(filter)
            .SortBy(r => r.EpisodeNumber)
            .ThenBy(r => r.ScheduledDate)
            .ToListAsync();
    }

    public async Task<ContentScheduleEntity?> GetByRegisteredContentIdAsync(string registeredContentId)
        => await _collection.Find(r => r.RegisteredContentId == registeredContentId).FirstOrDefaultAsync();

    public async Task<bool> ExistsByExternalKeyAsync(
        EContentScheduleType contentType, int tmdbId, int? seasonNumber, int? episodeNumber)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.ContentType, contentType),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.TmdbId, tmdbId),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.SeasonNumber, seasonNumber),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.EpisodeNumber, episodeNumber));

        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task<bool> UpdateScheduledDateAsync(string id, DateOnly scheduledDate)
    {
        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.Id, id),
            Builders<ContentScheduleEntity>.Filter.Ne(r => r.Status, EScheduleStatus.REGISTERED));

        var update = Builders<ContentScheduleEntity>.Update
            .Set(r => r.ScheduledDate, scheduledDate)
            .Set(r => r.UpdateAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<ContentScheduleEntity?> TryClaimAsync(string id, TimeSpan lease)
    {
        var now = DateTime.UtcNow;
        var leaseCutoff = now.Subtract(lease);

        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.Id, id),
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.NotificationProcessedAt, (DateTime?)null),
            Builders<ContentScheduleEntity>.Filter.Or(
                Builders<ContentScheduleEntity>.Filter.Eq(r => r.NotificationClaimedAt, (DateTime?)null),
                Builders<ContentScheduleEntity>.Filter.Lt(r => r.NotificationClaimedAt, leaseCutoff)));

        var update = Builders<ContentScheduleEntity>.Update
            .Set(r => r.NotificationClaimedAt, now)
            .Set(r => r.Status, EScheduleStatus.PENDING)
            .Inc(r => r.NotificationAttempts, 1)
            .Set(r => r.UpdateAt, now);

        return await _collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<ContentScheduleEntity> { ReturnDocument = ReturnDocument.After });
    }

    public async Task MarkProcessedAsync(string id)
    {
        var update = Builders<ContentScheduleEntity>.Update
            .Set(r => r.NotificationProcessedAt, DateTime.UtcNow)
            .Unset(r => r.NotificationClaimedAt)
            .Set(r => r.UpdateAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(r => r.Id == id, update);
    }

    public async Task ReleaseClaimAsync(string id)
    {
        var update = Builders<ContentScheduleEntity>.Update
            .Unset(r => r.NotificationClaimedAt)
            .Set(r => r.UpdateAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(r => r.Id == id, update);
    }

    public async Task ReleaseStaleClaimsAsync(TimeSpan lease)
    {
        var leaseCutoff = DateTime.UtcNow.Subtract(lease);

        var filter = Builders<ContentScheduleEntity>.Filter.And(
            Builders<ContentScheduleEntity>.Filter.Eq(r => r.NotificationProcessedAt, (DateTime?)null),
            Builders<ContentScheduleEntity>.Filter.Ne(r => r.NotificationClaimedAt, (DateTime?)null),
            Builders<ContentScheduleEntity>.Filter.Lt(r => r.NotificationClaimedAt, leaseCutoff));

        var update = Builders<ContentScheduleEntity>.Update
            .Unset(r => r.NotificationClaimedAt)
            .Set(r => r.UpdateAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update);
    }
}
