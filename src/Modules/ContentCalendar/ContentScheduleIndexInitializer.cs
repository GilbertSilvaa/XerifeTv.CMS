using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCalendar;

public sealed class ContentScheduleIndexInitializer(
    IMongoClient mongoClient,
    IOptions<DBSettings> dbSettings,
    ILogger<ContentScheduleIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = mongoClient
            .GetDatabase(dbSettings.Value.DatabaseName)
            .GetCollection<ContentScheduleEntity>(ECollection.CONTENT_SCHEDULES.ToString());

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys
                    .Ascending(x => x.ContentType)
                    .Ascending(x => x.TmdbId)
                    .Ascending(x => x.SeasonNumber)
                    .Ascending(x => x.EpisodeNumber),
                new CreateIndexOptions { Name = "ux_content_schedules_external_key", Unique = true }),
            "unique external key");

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys.Ascending(x => x.ScheduledDate),
                new CreateIndexOptions { Name = "ix_content_schedules_scheduled_date" }),
            "scheduled date");

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.ScheduledDate),
                new CreateIndexOptions { Name = "ix_content_schedules_status_date" }),
            "status and scheduled date");

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys.Ascending(x => x.ImdbId),
                new CreateIndexOptions { Name = "ix_content_schedules_imdb_id" }),
            "imdb id");

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys.Ascending(x => x.RegisteredContentId),
                new CreateIndexOptions { Name = "ix_content_schedules_registered_content_id" }),
            "registered content id");

        await CreateIndexAsync(
            collection,
            new CreateIndexModel<ContentScheduleEntity>(
                Builders<ContentScheduleEntity>.IndexKeys
                    .Ascending(x => x.NotificationProcessedAt)
                    .Ascending(x => x.ScheduledDate),
                new CreateIndexOptions { Name = "ix_content_schedules_notification_processed_date" }),
            "notification processing and scheduled date");
    }

    private async Task CreateIndexAsync(
        IMongoCollection<ContentScheduleEntity> collection,
        CreateIndexModel<ContentScheduleEntity> model,
        string description)
    {
        try
        {
            await collection.Indexes.CreateOneAsync(model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create {Description} index for content schedules", description);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
