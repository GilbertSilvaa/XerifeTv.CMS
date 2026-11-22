using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationCollectionSnapshotRepository(
    IOptions<DBSettings> dbSettings,
    IMongoClient mongoClient)
    : BaseRepository<CurationCollectionSnapshotEntity>(
        ECollection.CURATION_COLLECTION_SNAPSHOTS, dbSettings, mongoClient),
        ICurationCollectionSnapshotRepository
{
    public async Task<CurationCollectionSnapshotEntity?> GetByCollectionIdAsync(string collectionId)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
            return null;

        return await _collection
            .Find(Builders<CurationCollectionSnapshotEntity>.Filter.Eq(x => x.CollectionId, collectionId))
            .FirstOrDefaultAsync();
    }

    public async Task<CurationCollectionSnapshotEntity?> GetPageAsync(
        string collectionId, int skip, int take)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
            return null;

        var filter = Builders<CurationCollectionSnapshotEntity>.Filter
            .Eq(x => x.CollectionId, collectionId);

        var itemsSlice = new BsonDocument("$slice", new BsonArray
        {
            "$Items",
            Math.Max(0, skip),
            Math.Max(0, take)
        });

        var documents = await AggregateAsync(filter, itemsSlice);
        return documents.FirstOrDefault();
    }

    public async Task<IReadOnlyList<CurationCollectionSnapshotEntity>> GetFirstPageByCollectionIdsAsync(
        IEnumerable<string> collectionIds)
    {
        var idList = collectionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (idList.Count == 0) return [];

        var filter = Builders<CurationCollectionSnapshotEntity>.Filter
            .In(x => x.CollectionId, idList);

        var itemsSlice = new BsonDocument("$slice", new BsonArray
        {
            "$Items",
            "$PageSize"
        });

        return await AggregateAsync(filter, itemsSlice);
    }

    public async Task<IReadOnlyList<string>> GetAllCollectionIdsAsync()
    {
        return await _collection
            .Find(_ => true)
            .Project(x => x.CollectionId)
            .ToListAsync();
    }

    public async Task UpsertAsync(CurationCollectionSnapshotEntity snapshot)
    {
        var now = DateTime.UtcNow;

        var update = Builders<CurationCollectionSnapshotEntity>.Update
            .Set(x => x.CollectionName, snapshot.CollectionName)
            .Set(x => x.AllowsPagination, snapshot.AllowsPagination)
            .Set(x => x.PageSize, snapshot.PageSize)
            .Set(x => x.TotalCount, snapshot.TotalCount)
            .Set(x => x.CurationVersion, snapshot.CurationVersion)
            .Set(x => x.Items, snapshot.Items)
            .Set(x => x.UpdateAt, now)
            .SetOnInsert(x => x.Id, snapshot.Id)
            .SetOnInsert(x => x.CreateAt, snapshot.CreateAt);

        await _collection.UpdateOneAsync(
            Builders<CurationCollectionSnapshotEntity>.Filter
                .Eq(x => x.CollectionId, snapshot.CollectionId),
            update,
            new UpdateOptions { IsUpsert = true });
    }

    public async Task DeleteByCollectionIdAsync(string collectionId)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
            return;

        await _collection.DeleteOneAsync(
            Builders<CurationCollectionSnapshotEntity>.Filter
                .Eq(x => x.CollectionId, collectionId));
    }

    public async Task DeleteManyByCollectionIdsAsync(IEnumerable<string> collectionIds)
    {
        var idList = collectionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (idList.Count == 0) return;

        await _collection.DeleteManyAsync(
            Builders<CurationCollectionSnapshotEntity>.Filter
                .In(x => x.CollectionId, idList));
    }

    private async Task<IReadOnlyList<CurationCollectionSnapshotEntity>> AggregateAsync(
        FilterDefinition<CurationCollectionSnapshotEntity> filter,
        BsonValue itemsSlice)
    {
        var serializer = BsonSerializer.SerializerRegistry
            .GetSerializer<CurationCollectionSnapshotEntity>();

        var renderedFilter = filter.Render(
            serializer,
            BsonSerializer.SerializerRegistry);

        var pipeline = PipelineDefinition<CurationCollectionSnapshotEntity, BsonDocument>.Create(
            new BsonDocument("$match", renderedFilter),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "CreateAt", 1 },
                { "UpdateAt", 1 },
                { "CollectionId", 1 },
                { "CollectionName", 1 },
                { "AllowsPagination", 1 },
                { "PageSize", 1 },
                { "TotalCount", 1 },
                { "CurationVersion", 1 },
                { "Items", itemsSlice }
            }));

        var documents = await _collection
            .Aggregate(pipeline)
            .ToListAsync();

        return documents
            .Select(document => BsonSerializer.Deserialize<CurationCollectionSnapshotEntity>(document))
            .ToList();
    }
}
