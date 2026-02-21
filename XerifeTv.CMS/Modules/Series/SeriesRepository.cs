using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Series;

public sealed class SeriesRepository(IOptions<DBSettings> options)
  : BaseRepository<SeriesEntity>(ECollection.SERIES, options), ISeriesRepository
{
    public override async Task<PagedList<SeriesEntity>> GetAsync(int currentPage, int limit)
    {
        var projection = Builders<SeriesEntity>.Projection.Exclude(r => r.Episodes);

        var count = await _collection.CountDocumentsAsync(_ => true);
        var items = await _collection.Find(_ => true)
          .Project<SeriesEntity>(projection)
          .SortByDescending(r => r.CreateAt)
          .Skip(limit * (currentPage - 1))
          .Limit(limit)
          .ToListAsync();

        var totalPages = (int)Math.Ceiling(count / (decimal)limit);

        return new PagedList<SeriesEntity>(currentPage, totalPages, items);
    }

    public override async Task<SeriesEntity?> GetAsync(string id)
    {
        var projection = Builders<SeriesEntity>.Projection.Exclude(r => r.Episodes);

        var response = await _collection
          .Find(r => r.Id == id)
          .Project<SeriesEntity>(projection)
          .FirstOrDefaultAsync();

        return response;
    }

    public async Task<PagedList<SeriesEntity>> GetByFilterAsync(GetSeriesByFilterRequestDto dto)
    {
        Expression<Func<SeriesEntity, bool>> filterExpression = dto.Filter switch
        {
            ESeriesSearchFilter.CATEGORY => r =>
              r.Categories.Any(x =>
                x.Equals(dto.Search.Trim(), StringComparison.CurrentCultureIgnoreCase)) && (!r.Disabled || dto.IsIncludeDisabled),

            _ => r =>
              r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled)
        };

        FilterDefinition<SeriesEntity> filter = Builders<SeriesEntity>.Filter.Where(filterExpression);
        var projection = Builders<SeriesEntity>.Projection.Exclude(r => r.Episodes);

        var count = await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
          .Project<SeriesEntity>(projection)
          .SortBy(r => r.Title)
          .Skip(dto.LimitResults * (dto.CurrentPage - 1))
          .Limit(dto.LimitResults)
          .ToListAsync();

        var totalPages = (int)Math.Ceiling(count / (decimal)dto.LimitResults);

        return new PagedList<SeriesEntity>(dto.CurrentPage, totalPages, items);
    }

    public async Task<SeriesEntity?> GetByImdbIdAsync(string imdbId)
    {
        return await _collection
          .Find(r => r.ImdbId == imdbId)
          .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ItemsByCategory<SeriesEntity>>> GetGroupByCategoryAsync(GetGroupByCategoryRequestDto dto)
    {
        List<ItemsByCategory<SeriesEntity>> result = [];
        List<string> uniqueSeriesIds = [];

        foreach (var category in dto.Categories)
        {
            var seriesByCategory = await _collection
              .Find(r => r.Categories.Any(x => x.Equals(category)) && !uniqueSeriesIds.Contains(r.Id) && (!r.Disabled || dto.IsIncludeDisabled))
              .SortByDescending(x => x.CreateAt)
              .Skip(dto.LimitResults * (dto.CurrentPage - 1))
              .Limit(dto.LimitResults)
              .ToListAsync();

            uniqueSeriesIds.AddRange(seriesByCategory.Select(x => x.Id));

            if (seriesByCategory.Any())
                result.Add(new ItemsByCategory<SeriesEntity>(category, seriesByCategory));
        }

        return result;
    }

    public async Task<SeriesEntity?> GetEpisodesBySeasonAsync(string serieId, int season, bool includeDisabled, int? specificEpisode = null)
    {
        var filter = Builders<SeriesEntity>.Filter.Eq(r => r.Id, serieId);
        var projection = Builders<SeriesEntity>.Projection.Expression(
          r => new SeriesEntity
          {
              Id = r.Id,
              Title = r.Title,
              NumberSeasons = r.NumberSeasons,
              Episodes = r.Episodes
              .Where(e => e.Season == season && (!e.Disabled || includeDisabled) && (e.Number == specificEpisode || specificEpisode == null))
              .OrderBy(e => e.Number)
              .ToList()
          }
        );

        var response = await _collection
          .Find(filter)
          .Project(projection)
          .FirstOrDefaultAsync();

        return response;
    }

    public override async Task UpdateAsync(SeriesEntity entity)
    {
        var filter = Builders<SeriesEntity>.Filter.Eq(r => r.Id, entity.Id);

        var update = Builders<SeriesEntity>.Update
          .Set(r => r.ImdbId, entity.ImdbId)
          .Set(r => r.Title, entity.Title)
          .Set(r => r.Categories, entity.Categories)
          .Set(r => r.Categories, entity.Categories)
          .Set(r => r.Synopsis, entity.Synopsis)
          .Set(r => r.Review, entity.Review)
          .Set(r => r.PosterUrl, entity.PosterUrl)
          .Set(r => r.BannerUrl, entity.BannerUrl)
          .Set(r => r.NumberSeasons, entity.NumberSeasons)
          .Set(r => r.ReleaseYear, entity.ReleaseYear)
          .Set(r => r.ParentalRating, entity.ParentalRating)
          .Set(r => r.UpdateAt, DateTime.UtcNow)
          .Set(r => r.Disabled, entity.Disabled);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<string> CreateEpisodeAsync(string serieId, Episode episode)
    {
        var filter = Builders<SeriesEntity>.Filter.Eq(r => r.Id, serieId);
        var update = Builders<SeriesEntity>.Update.Push(r => r.Episodes, episode);

        await _collection.UpdateOneAsync(filter, update);

        return episode.Id;
    }

    public async Task UpdateEpisodeAsync(string serieId, Episode episode)
    {
        var exist = await DeleteEpisodeAsync(serieId, episode.Id);
        if (!exist) return;

        await CreateEpisodeAsync(serieId, episode);
    }

    public async Task<bool> DeleteEpisodeAsync(string serieId, string episodeId)
    {
        var filter = Builders<SeriesEntity>.Filter.Eq(r => r.Id, serieId);

        var update = Builders<SeriesEntity>.Update.PullFilter(
          r => r.Episodes,
          e => e.Id == episodeId);

        var response = await _collection.UpdateOneAsync(filter, update);

        return response.ModifiedCount > 0;
    }

    public async Task<ICollection<string>> GetAllCategoriesAsync()
    {
        var categories = await _collection
            .DistinctAsync<string>("Categories", FilterDefinition<SeriesEntity>.Empty);

        var list = await categories.ToListAsync();

        var rng = new Random(DateTime.UtcNow.DayOfYear);
        return [.. list.OrderBy(x => rng.Next())];
    }
}