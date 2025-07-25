﻿using Microsoft.Extensions.Options;
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
              r.ReleaseYear.Equals(int.Parse(dto.Search)) && (!r.Disabled || dto.IsIncludeDisabled),

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
              .Find(r => r.Categories.Any(x => x.Equals(category)) && !uniqueMovieIds.Contains(r.Id))
              .SortByDescending(x => x.CreateAt)
              .Skip(dto.LimitResults * (dto.CurrentPage - 1))
              .Limit(dto.LimitResults)
              .ToListAsync();

            uniqueMovieIds.AddRange(moviesByCategory.Select(x => x.Id));

            if (moviesByCategory.Any())
                result.Add(new ItemsByCategory<MovieEntity>(category, moviesByCategory));
        }

        return result;
    }
}