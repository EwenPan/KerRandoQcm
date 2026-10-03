using System.Linq.Expressions;
using KerRandoQcm.Models;
using MongoDB.Driver;

namespace KerRandoQcm.Data.Mongo;

/// <summary>IRepository implementation backed by a real MongoDB collection.</summary>
public class MongoRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public Task<List<T>> GetAllAsync() =>
        _collection.Find(Builders<T>.Filter.Empty).ToListAsync();

    public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate) =>
        _collection.Find(predicate).FirstOrDefaultAsync()!;

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        _collection.Find(predicate).ToListAsync();

    public Task<long> CountAsync() =>
        _collection.CountDocumentsAsync(Builders<T>.Filter.Empty);

    public Task InsertOneAsync(T entity) => _collection.InsertOneAsync(entity);

    public Task ReplaceOneAsync(T entity) => _collection.ReplaceOneAsync(item => item.Id == entity.Id, entity);

    public Task InsertManyAsync(IEnumerable<T> entities) => _collection.InsertManyAsync(entities);

    public Task DeleteOneAsync(Expression<Func<T, bool>> predicate) => _collection.DeleteOneAsync(predicate);

    public Task DeleteAllAsync() => _collection.DeleteManyAsync(Builders<T>.Filter.Empty);

    public async Task UpdateOneAsync(Expression<Func<T, bool>> predicate, Action<T> apply)
    {
        var entity = await _collection.Find(predicate).FirstOrDefaultAsync();
        if (entity == null) return;

        apply(entity);
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
    }

    public async Task UpdateAllAsync(Action<T> apply)
    {
        var entities = await _collection.Find(Builders<T>.Filter.Empty).ToListAsync();
        foreach (var entity in entities)
        {
            apply(entity);
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        }
    }
}
