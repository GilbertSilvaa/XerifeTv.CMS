using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Abstractions.Repositories;
using XerifeTv.CMS.Models.Channel.Dtos.Request;
using XerifeTv.CMS.Models.Channel.Interfaces;
using XerifeTv.CMS.Models.Channel.Enums;
using XerifeTv.CMS.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace XerifeTv.CMS.Models.Channel;

public sealed class ChannelRepository(IOptions<DBSettings> options)
  : BaseRepository<ChannelEntity>(ECollection.CHANNELS, options), IChannelRepository
{
  public async Task<PagedList<ChannelEntity>> GetByFilterAsync(GetChannelsByFilterRequestDto dto)
  {
    Expression<Func<ChannelEntity, bool>> filterExpression = dto.Filter switch
    {
      EChannelSearchFilter.TITLE => r => 
        r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled),
      
      EChannelSearchFilter.CATEGORY => r => 
        r.Category.Equals(dto.Search.Trim(), StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled),
      
      _ => r => 
        r.Title.Contains(dto.Search, StringComparison.CurrentCultureIgnoreCase) && (!r.Disabled || dto.IsIncludeDisabled)
    };

    FilterDefinition<ChannelEntity> filter = Builders<ChannelEntity>.Filter.Where(filterExpression);

    var count = await _collection.CountDocumentsAsync(filter);
    var items = await _collection.Find(filter)
      .SortBy(r => r.Title)
      .Skip(dto.LimitResults * (dto.CurrentPage - 1))
      .Limit(dto.LimitResults)
      .ToListAsync();

    var totalPages = (int)Math.Ceiling(count / (decimal)dto.LimitResults);

    return new PagedList<ChannelEntity>(dto.CurrentPage, totalPages, items);
  }

  public async Task<IEnumerable<ItemsByCategory<ChannelEntity>>> GetGroupByCategoryAsync(int limit)
  {
    return await _collection
      .Aggregate()
      .Group(
        r => r.Category,
        g => new ItemsByCategory<ChannelEntity>(
          g.Key, 
          g.Where(x => !x.Disabled).OrderByDescending(x => x.CreateAt).Take(limit).ToList()))
      .Match(g => g.Items.Any())
      .ToListAsync();
  }
}