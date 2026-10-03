using System.Linq.Expressions;
using KerRandoQcm.Models;

namespace KerRandoQcm.Data;

/// <summary>
/// Storage-agnostic CRUD contract implemented by both the MongoDB-backed and the in-memory data stores,
/// so the rest of the app never depends on MongoDB.Driver types directly.
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<long> CountAsync();
    Task InsertOneAsync(T entity);
    Task ReplaceOneAsync(T entity);
    Task InsertManyAsync(IEnumerable<T> entities);
    Task DeleteOneAsync(Expression<Func<T, bool>> predicate);
    Task DeleteAllAsync();
    Task UpdateOneAsync(Expression<Func<T, bool>> predicate, Action<T> apply);
    Task UpdateAllAsync(Action<T> apply);
}
