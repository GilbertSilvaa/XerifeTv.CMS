using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Abstractions.Repositories;
using XerifeTv.CMS.Models.Movie.Dtos.Request;
using XerifeTv.CMS.Models.Movie.Enums;
using XerifeTv.CMS.Models.Movie.Interfaces;
using XerifeTv.CMS.MongoDB;

namespace XerifeTv.CMS.Models.Movie;

public sealed class MovieRepository(IOptions<DBSettings> options) 
  : BaseRepository<MovieEntity>(ECollection.MOVIES, options), IMovieRepository
{
  public async Task<PagedList<MovieEntity>> GetByFilterAsync(GetMoviesByFilterRequestDto dto)
  {
    Expression<Func<MovieEntity, bool>> filterExpression = dto.Filter switch
    {
      EMovieSearchFilter.TITLE => r => r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase),
      EMovieSearchFilter.CATEGORY => r => r.Category.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase),
      EMovieSearchFilter.RELEASE_YEAR => r => r.ReleaseYear.Equals(int.Parse(dto.Search)),
      _ => r => r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase)
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

  public async Task<IEnumerable<ItemsByCategory<MovieEntity>>> GetGroupByCategoryAsync(int limit)
  {
    return await _collection
      .Aggregate()
      .Group(
        r => r.Category, 
        g => 
          new ItemsByCategory<MovieEntity>(g.Key, g.OrderByDescending(x => x.CreateAt).Take(limit).ToList()))
      .ToListAsync();
  }
}