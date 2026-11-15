using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationIndexInitializer(
    IMongoClient mongoClient,
    IOptions<DBSettings> dbSettings,
    ILogger<CurationIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

        try
        {
            await database.GetCollection<CurationCollectionEntity>(ECollection.CURATION_COLLECTIONS.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<CurationCollectionEntity>(
                        Builders<CurationCollectionEntity>.IndexKeys.Ascending(x => x.Name),
                        new CreateIndexOptions<CurationCollectionEntity>
                        {
                            Name = "ux_curation_collections_name",
                            Unique = true,
                            Collation = new Collation("en", strength: CollationStrength.Secondary)
                        }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create unique index for curation collection names");
        }

        try
        {
            await database.GetCollection<CurationCollectionEntity>(ECollection.CURATION_COLLECTIONS.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<CurationCollectionEntity>(
                        Builders<CurationCollectionEntity>.IndexKeys
                            .Ascending(x => x.IsActive)
                            .Ascending(x => x.Order),
                        new CreateIndexOptions { Name = "ix_curation_collections_active_order" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create index for curation collections activation and order");
        }

        try
        {
            await database.GetCollection<FeaturedEntity>(ECollection.CURATION_FEATUREDS.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<FeaturedEntity>(
                        Builders<FeaturedEntity>.IndexKeys
                            .Ascending(x => x.IsActive)
                            .Ascending(x => x.Order),
                        new CreateIndexOptions { Name = "ix_curation_featureds_active_order" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create index for curation featureds activation and order");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
