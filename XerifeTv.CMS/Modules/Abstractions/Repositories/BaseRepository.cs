using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Abstractions.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
	protected readonly IMongoCollection<T> _collection;

	public BaseRepository(ECollection collection, IOptions<DBSettings> dbSettings)
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

		var settings = MongoClientSettings.FromConnectionString(dbSettings.Value.ConnectionString);
		settings.SslSettings = new SslSettings { CheckCertificateRevocation = false };

		var _mongoClient = new MongoClient(settings);
		var _mongoDB = _mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
		_collection = _mongoDB.GetCollection<T>(collection.ToString());
	}

	public virtual async Task<PagedList<T>> GetAsync(int currentPage, int limit)
	{
		var count = await _collection.CountDocumentsAsync(_ => true);
		var items = await _collection.Find(_ => true)
		  .SortByDescending(r => r.CreateAt)
		  .Skip(limit * (currentPage - 1))
		  .Limit(limit)
		  .ToListAsync();

		var totalPages = (int)Math.Ceiling(count / (decimal)limit);

		return new PagedList<T>(currentPage, totalPages, items);
	}

	public virtual async Task<T?> GetAsync(string id)
	  => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

	public virtual async Task<string> CreateAsync(T entity)
	{
		await _collection.InsertOneAsync(entity);
		return entity.Id;
	}

	public virtual async Task UpdateAsync(T entity)
	{
		entity.UpdateAt = DateTime.UtcNow;
		await _collection.ReplaceOneAsync(r => r.Id == entity.Id, entity);
	}

	public virtual async Task DeleteAsync(string id)
	  => await _collection.DeleteOneAsync(r => r.Id == id);

	public async Task<long> CountAsync()
	  => await _collection.CountDocumentsAsync(_ => true);
}
