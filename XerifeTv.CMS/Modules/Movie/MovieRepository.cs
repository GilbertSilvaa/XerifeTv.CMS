using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Movie;

public sealed class MovieRepository(IOptions<DBSettings> options)
  : BaseRepository<MovieEntity>(ECollection.MOVIES, options), IMovieRepository
{
    public async Task<PagedList<MovieEntity>> GetByFilterAsync(GetMoviesByFilterRequestDto dto)
    {
        Expression<Func<MovieEntity, bool>> filterExpression = dto.Filter switch
        {
            EMovieSearchFilter.TITLE => r =>
              r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled),

            EMovieSearchFilter.CATEGORY => r =>
              r.Categories.Any(x =>
                x.Equals(dto.Search.Trim(), StringComparison.CurrentCultureIgnoreCase)) && (!r.Disabled || dto.IsIncludeDisabled),

            EMovieSearchFilter.RELEASE_YEAR => r =>
              r.ReleaseYear.ToString() == dto.Search && (!r.Disabled || dto.IsIncludeDisabled),

            EMovieSearchFilter.PARENTAL_RATING => r =>
              r.ParentalRating.ToString() == dto.Search && (!r.Disabled || dto.IsIncludeDisabled),

            EMovieSearchFilter.ONLY_DISABLED => r => r.Disabled,

            _ => r =>
              r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled)
        };

        FilterDefinition<MovieEntity> filter = Builders<MovieEntity>.Filter.Where(filterExpression);
        var count = await _collection.CountDocumentsAsync(filter);

        var query = _collection.Find(filter)
          .Skip(dto.LimitResults * (dto.CurrentPage - 1))
          .Limit(dto.LimitResults);

        query = dto.Order == EMovieOrderFilter.REGISTRATION_DATE_DESC
          ? query.SortByDescending(r => r.CreateAt)
          : query.SortBy(r => r.Title);

        var items = await query.ToListAsync();
        var totalPages = (int)Math.Ceiling(count / (decimal)dto.LimitResults);

        return new PagedList<MovieEntity>(dto.CurrentPage, totalPages, items);
    }

    public async Task<MovieEntity?> GetByImdbIdAsync(string imdbId)
    {
        return await _collection
          .Find(r => r.ImdbId == imdbId)
          .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ItemsByCategory<MovieEntity>>> GetGroupByCategoryAsync(GetGroupByCategoryRequestDto dto)
    {
        List<ItemsByCategory<MovieEntity>> result = [];
        List<string> uniqueMovieIds = [];

        foreach (var category in dto.Categories)
        {
            var moviesByCategory = await _collection
              .Find(r => r.Categories.Any(x => x.Equals(category)) && (!r.Disabled || dto.IsIncludeDisabled))
              .SortByDescending(x => x.CreateAt)
              .Skip(dto.LimitResults * (dto.CurrentPage - 1))
              .Limit(dto.LimitResults)
              .ToListAsync();

            var orderedMovies = moviesByCategory
                .OrderBy(x => uniqueMovieIds.Contains(x.Id))
                .ThenByDescending(x => x.CreateAt)
                .ToList();

            uniqueMovieIds.AddRange(moviesByCategory.Select(x => x.Id));

            if (moviesByCategory.Any())
                result.Add(new ItemsByCategory<MovieEntity>(category, orderedMovies));
        }

        return result;
    }

    public async Task<ICollection<CategoryCountDto>> GetCategoriesWithCountAsync()
    {
        var result = await _collection
            .Aggregate()
            .Unwind<MovieEntity, BsonDocument>(x => x.Categories)
            .Group(new BsonDocument
            {
                { "_id", "$Categories" },
                { "count", new BsonDocument("$sum", 1) }
            })
            .Sort(new BsonDocument("count", -1))
            .ToListAsync();

        return [.. result.Select(x => new CategoryCountDto
            {
                Category = x["_id"].AsString,
                Count = x["count"].AsInt32
            })];
    }

    public async Task<ICollection<MovieEntity>> GetMoviesRecommendedByMovieIdAsync(string movieId, int limit)
    {
        var movie = await _collection.Find(r => r.Id == movieId).FirstOrDefaultAsync();
        if (movie == null) return Array.Empty<MovieEntity>();

        var baseCategories = movie.Categories;

        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "_id", new BsonDocument("$ne", movie.Id) }
            }),

            new BsonDocument("$addFields", new BsonDocument
            {
                { "MatchingCount", new BsonDocument
                    {
                        { "$size", new BsonDocument
                            {
                                { "$setIntersection", new BsonArray
                                    {
                                        "$Categories", new BsonArray(baseCategories)
                                    }
                                }
                            }
                        }
                    }
                }
            }),

            new BsonDocument("$match", new BsonDocument
            {
                { "MatchingCount", new BsonDocument("$gt", 0) },
                { "Disabled", false }
            }),

            new BsonDocument("$sort", new BsonDocument
            {
                { "MatchingCount", -1 },
                { "Review", -1 }
            }),

            new BsonDocument("$project", new BsonDocument
            {
                { "MatchingCount", 0 }
            }),

            new BsonDocument("$limit", limit)
        };

        var recommendedMovies = await _collection.Aggregate<MovieEntity>(pipeline).ToListAsync();

        return recommendedMovies;
    }
}