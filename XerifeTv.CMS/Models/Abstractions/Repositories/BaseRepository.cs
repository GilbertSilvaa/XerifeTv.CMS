﻿using XerifeTv.CMS.Models.Abstractions.Entities;
using MongoDB.Driver;
using XerifeTv.CMS.MongoDB;
using Microsoft.Extensions.Options;

namespace XerifeTv.CMS.Models.Abstractions.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
  protected readonly IMongoCollection<T> _collection;

  public BaseRepository(ECollection collection, IOptions<DBSettings> dbSettings)
  {
    var _mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
    var _mongoDB = _mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
    _collection = _mongoDB.GetCollection<T>(collection.ToString());
  }

  public virtual async Task<PagedList<T>> GetAsync(int currentPage, int limit)
  {
    var count = await _collection.CountDocumentsAsync(_ => true);
    var items = await _collection.Find(_ => true)
      .Skip(limit * (currentPage - 1))
      .Limit(limit)
      .ToListAsync();

    var totalPages = (int)Math.Ceiling(count / (decimal)limit);

    return new PagedList<T>(currentPage, totalPages, items);
  }
  
  public virtual async Task<T?> GetAsync(string id)
    => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

  public virtual async Task CreateAsync(T entity)
    => await _collection.InsertOneAsync(entity);

  public virtual async Task UpdateAsync(T entity)
    => await _collection.ReplaceOneAsync(r => r.Id == entity.Id, entity);

  public virtual async Task DeleteAsync(string id)
    => await _collection.DeleteOneAsync(r => r.Id == id);
}
