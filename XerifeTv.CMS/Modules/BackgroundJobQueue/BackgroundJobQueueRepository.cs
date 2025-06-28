using Microsoft.Extensions.Options;
using MongoDB.Driver;
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
		FilterDefinition<BackgroundJobEntity> filter = Builders<BackgroundJobEntity>.Filter
			.Where(r => (r.Status == dto.Status || dto.Status == null) 
				&& (r.RequestedByUserId == dto.ResponsibleUserId || dto.ResponsibleUserId == null)
				&& r.CreateAt >= DateTime.UtcNow.AddDays(-30));

		var count = await _collection.CountDocumentsAsync(filter);

		var query = _collection.Find(filter)
		  .Skip(dto.LimitResults * (dto.CurrentPage - 1))
		  .Limit(dto.LimitResults);

		query = dto.Order == EBackgroundJobOrderFilter.REGISTRATION_DATE_DESC
		  ? query.SortByDescending(r => r.CreateAt)
		  : query.SortBy(r => r.CreateAt);

		var items = await query.ToListAsync();
		var totalPages = (int)Math.Ceiling(count / (decimal)dto.LimitResults);

		return new PagedList<BackgroundJobEntity>(dto.CurrentPage, totalPages, items);
	}
}
