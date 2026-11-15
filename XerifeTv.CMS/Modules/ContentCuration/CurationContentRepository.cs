using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Text.RegularExpressions;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Filters;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationContentRepository : ICurationContentRepository
{
    private readonly IMongoCollection<MovieEntity> _movies;
    private readonly IMongoCollection<SeriesEntity> _series;

    public CurationContentRepository(IOptions<DBSettings> dbSettings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
        _movies = database.GetCollection<MovieEntity>(ECollection.MOVIES.ToString());
        _series = database.GetCollection<SeriesEntity>(ECollection.SERIES.ToString());
    }

    public async Task<IReadOnlyList<MovieEntity>> GetMoviesByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        var filter = Builders<MovieEntity>.Filter.In(x => x.Id, idList)
            & Builders<MovieEntity>.Filter.Ne(x => x.Disabled, true);

        return await _movies.Find(filter).ToListAsync();
    }

    public async Task<IReadOnlyList<SeriesEntity>> GetSeriesByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        var filter = Builders<SeriesEntity>.Filter.In(x => x.Id, idList)
            & Builders<SeriesEntity>.Filter.Ne(x => x.Disabled, true);

        return await _series.Find(filter).ToListAsync();
    }

    public async Task<IReadOnlyList<MovieEntity>> GetMoviesAsync(DynamicCollectionQuery query)
    {
        var filter = BuildFilter<MovieEntity>(query, EContentType.MOVIE);
        var sort = BuildSort<MovieEntity>(query);

        return await _movies
            .Find(filter)
            .Sort(sort)
            .Limit(Math.Max(1, query.Limit))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SeriesEntity>> GetSeriesAsync(DynamicCollectionQuery query)
    {
        var filter = BuildFilter<SeriesEntity>(query, EContentType.SERIES);
        var sort = BuildSort<SeriesEntity>(query);

        return await _series
            .Find(filter)
            .Sort(sort)
            .Limit(Math.Max(1, query.Limit))
            .ToListAsync();
    }

    public Task<long> CountMoviesAsync(DynamicCollectionQuery query)
    {
        var filter = BuildFilter<MovieEntity>(query, EContentType.MOVIE);
        return _movies.CountDocumentsAsync(filter);
    }

    public Task<long> CountSeriesAsync(DynamicCollectionQuery query)
    {
        var filter = BuildFilter<SeriesEntity>(query, EContentType.SERIES);
        return _series.CountDocumentsAsync(filter);
    }

    public async Task<IReadOnlyList<MovieEntity>> SearchMoviesAsync(string term, int limit, bool includeDisabled = true)
    {
        var filter = Builders<MovieEntity>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(term))
            filter &= Builders<MovieEntity>.Filter.Regex(
                x => x.Title, new BsonRegularExpression(Regex.Escape(term.Trim()), "i"));

        if (!includeDisabled)
            filter &= Builders<MovieEntity>.Filter.Eq(x => x.Disabled, false);

        return await _movies
            .Find(filter)
            .SortByDescending(x => x.CreateAt)
            .Limit(Math.Max(1, limit))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SeriesEntity>> SearchSeriesAsync(string term, int limit, bool includeDisabled = true)
    {
        var filter = Builders<SeriesEntity>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(term))
            filter &= Builders<SeriesEntity>.Filter.Regex(
                x => x.Title, new BsonRegularExpression(Regex.Escape(term.Trim()), "i"));

        if (!includeDisabled)
            filter &= Builders<SeriesEntity>.Filter.Eq(x => x.Disabled, false);

        return await _series
            .Find(filter)
            .SortByDescending(x => x.CreateAt)
            .Limit(Math.Max(1, limit))
            .ToListAsync();
    }

    private static FilterDefinition<T> BuildFilter<T>(DynamicCollectionQuery query, EContentType contentType)
        where T : BaseEntity
    {
        var enabled = Builders<T>.Filter.Ne(new StringFieldDefinition<T, bool>("Disabled"), true);

        if (query.Filters.Count == 0)
            return enabled;

        var conditions = query.Filters
            .Select(rule => BuildCondition<T>(rule, contentType))
            .ToList();

        var combined = query.LogicalOperator == ECurationLogicalOperator.OR
            ? Builders<T>.Filter.Or(conditions)
            : Builders<T>.Filter.And(conditions);

        return Builders<T>.Filter.And(enabled, combined);
    }

    private static FilterDefinition<T> BuildCondition<T>(DynamicFilterRule rule, EContentType contentType)
        where T : BaseEntity
    {
        var property = CurationFilterCatalog.Find(rule.Property)
            ?? throw new InvalidOperationException($"Propriedade de filtro não autorizada: {rule.Property}.");

        var field = property.FieldFor(contentType)
            ?? throw new InvalidOperationException($"Propriedade {rule.Property} não é compatível com {contentType}.");

        if (!CurationFilterCatalog.SupportsOperator(property.ValueType, rule.Operator))
            throw new InvalidOperationException($"Operador {rule.Operator} não é válido para a propriedade {rule.Property}.");

        var value = ParseValue(rule.Value, property.ValueType);
        var secondValue = ParseValue(rule.SecondValue, property.ValueType);

        if (value is null)
            throw new InvalidOperationException($"Valor obrigatório não informado para a propriedade {rule.Property}.");

        return rule.Operator switch
        {
            ECurationFilterOperator.EQUALS => Builders<T>.Filter
                .Eq(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.NOT_EQUALS => Builders<T>.Filter
                .Ne(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.CONTAINS => BuildContains<T>(field, value, property.ValueType),

            ECurationFilterOperator.NOT_CONTAINS => Builders<T>.Filter
                .Not(BuildContains<T>(field, value, property.ValueType)),

            ECurationFilterOperator.GREATER_THAN => Builders<T>.Filter
                .Gt(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.LESS_THAN => Builders<T>.Filter
                .Lt(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.GREATER_THAN_OR_EQUAL => Builders<T>.Filter
                .Gte(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.LESS_THAN_OR_EQUAL => Builders<T>.Filter
                .Lte(new StringFieldDefinition<T, object>(field), value),

            ECurationFilterOperator.BETWEEN => Builders<T>.Filter.And(
                Builders<T>.Filter.Gte(new StringFieldDefinition<T, object>(field), value),
                Builders<T>.Filter.Lte(new StringFieldDefinition<T, object>(field), secondValue
                    ?? throw new InvalidOperationException($"Segundo valor obrigatório para o operador BETWEEN em {rule.Property}."))),

            _ => throw new InvalidOperationException($"Operador de filtro não suportado: {rule.Operator}.")
        };
    }

    private static FilterDefinition<T> BuildContains<T>(string field, object value, ECurationFilterValueType valueType)
    {
        if (valueType == ECurationFilterValueType.LIST)
            return Builders<T>.Filter.AnyEq(
                new StringFieldDefinition<T, IEnumerable<string>>(field), (string)value);

        return Builders<T>.Filter.Regex(
            new StringFieldDefinition<T, object>(field),
            new BsonRegularExpression(Regex.Escape((string)value), "i"));
    }

    private static SortDefinition<T> BuildSort<T>(DynamicCollectionQuery query)
    {
        var field = query.OrderBy switch
        {
            ECurationOrderBy.TITLE => "Title",
            ECurationOrderBy.RELEASE_YEAR => "ReleaseYear",
            ECurationOrderBy.REVIEW => "Review",
            _ => "CreateAt"
        };

        var direction = query.SortDirection == ECurationSortDirection.ASC ? 1 : -1;

        var document = new BsonDocument
        {
            { field, direction },
            { "_id", 1 }
        };

        return new BsonDocumentSortDefinition<T>(document);
    }

    private static object? ParseValue(string? raw, ECurationFilterValueType valueType)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return valueType switch
        {
            ECurationFilterValueType.TEXT or ECurationFilterValueType.LIST
                or ECurationFilterValueType.REFERENCE => raw,
            ECurationFilterValueType.NUMBER => double.Parse(raw, CultureInfo.InvariantCulture),
            ECurationFilterValueType.DATE => DateTime.Parse(
                raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
            _ => raw
        };
    }
}
