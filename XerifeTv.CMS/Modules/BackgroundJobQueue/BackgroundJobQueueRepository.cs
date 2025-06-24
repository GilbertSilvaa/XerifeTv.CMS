using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobQueueRepository(IOptions<DBSettings> options)
	: BaseRepository<BackgroundJobEntity>(ECollection.BACKGROUND_JOB_QUEUE, options), IBackgroundJobQueueRepository
{
	public async Task<PagedList<BackgroundJobEntity>> GetByFilterAsync(GetBackgroundJobsByFilterRequestDto dto)
	{
		Expression<Func<BackgroundJobEntity, bool>> filterExpression = dto.Filter switch
		{
			EBackgroundJobSearchFilter.REQUESTING_USER => r => r.RequestedByUserId.Equals(dto.Search, StringComparison.OrdinalIgnoreCase),

			EBackgroundJobSearchFilter.STATUS => r => (int)r.Status == int.Parse(dto.Search),

			_ => r => r.RequestedByUserId.Equals(dto.Search, StringComparison.OrdinalIgnoreCase)
		};

		FilterDefinition<BackgroundJobEntity> filter = Builders<BackgroundJobEntity>.Filter.Where(filterExpression);

		var count = await _collection.CountDocumentsAsync(filter);
		var items = await _collection.Find(filter)
		  .SortBy(r => r.CreateAt)
		  .Skip(dto.LimitResults * (dto.CurrentPage - 1))
		  .Limit(dto.LimitResults)
		  .ToListAsync();

		var totalPages = (int)Math.Ceiling(count / (decimal)dto.LimitResults);

		return new PagedList<BackgroundJobEntity>(dto.CurrentPage, totalPages, items);
	}
}
