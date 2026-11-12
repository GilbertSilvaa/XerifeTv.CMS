using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.People;

public sealed class PeopleIndexInitializer(
    IMongoClient mongoClient,
    IOptions<DBSettings> dbSettings,
    ILogger<PeopleIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            await database.GetCollection<PersonEntity>(ECollection.PEOPLE.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<PersonEntity>(
                        Builders<PersonEntity>.IndexKeys.Ascending(x => x.TmdbId),
                        new CreateIndexOptions<PersonEntity>
                        {
                            Name = "ux_people_tmdbId",
                            Unique = true,
                            PartialFilterExpression = Builders<PersonEntity>.Filter.Type("TmdbId", BsonType.Int32)
                        }));

            await database.GetCollection<MovieEntity>(ECollection.MOVIES.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<MovieEntity>(
                        Builders<MovieEntity>.IndexKeys.Ascending("Credits.PersonId"),
                        new CreateIndexOptions { Name = "ix_movies_credits_personId" }));

            await database.GetCollection<SeriesEntity>(ECollection.SERIES.ToString())
                .Indexes.CreateOneAsync(
                    new CreateIndexModel<SeriesEntity>(
                        Builders<SeriesEntity>.IndexKeys.Ascending("Credits.PersonId"),
                        new CreateIndexOptions { Name = "ix_series_credits_personId" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create people/credits MongoDB indexes");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
