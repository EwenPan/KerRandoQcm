using System.Linq.Expressions;
using KerRandoQcm.Models;

namespace KerRandoQcm.Data.InMemory;

/// <summary>
/// IRepository implementation that keeps everything in a local in-process list. Used for local
/// development when MongoDB is not installed. Data is lost when the app stops.
/// </summary>
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly List<T> _items = new();
    private readonly object _lock = new();

    public Task<List<T>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_items.ToList());
        }
    }

    public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            var match = _items.AsQueryable().FirstOrDefault(predicate);
            return Task.FromResult(match);
        }
    }

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            var matches = _items.AsQueryable().Where(predicate).ToList();
            return Task.FromResult(matches);
        }
    }

    public Task<long> CountAsync()
    {
        lock (_lock)
        {
            return Task.FromResult((long)_items.Count);
        }
    }

    public Task InsertOneAsync(T entity)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = Guid.NewGuid().ToString("N");
            }
            _items.Add(entity);
        }
        return Task.CompletedTask;
    }

    public Task InsertManyAsync(IEnumerable<T> entities)
    {
        lock (_lock)
        {
            foreach (var entity in entities)
            {
                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString("N");
                }
                _items.Add(entity);
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteOneAsync(Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            var match = _items.AsQueryable().FirstOrDefault(predicate);
            if (match != null)
            {
                _items.Remove(match);
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync()
    {
        lock (_lock)
        {
            _items.Clear();
        }
        return Task.CompletedTask;
    }

    public Task ReplaceOneAsync(T entity)
    {
        lock (_lock)
        {
            var index = _items.FindIndex(item => item.Id == entity.Id);
            if (index >= 0) _items[index] = entity;
        }
        return Task.CompletedTask;
    }

    public Task UpdateOneAsync(Expression<Func<T, bool>> predicate, Action<T> apply)
    {
        lock (_lock)
        {
            var match = _items.AsQueryable().FirstOrDefault(predicate);
            if (match != null)
            {
                apply(match);
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateAllAsync(Action<T> apply)
    {
        lock (_lock)
        {
            foreach (var item in _items)
            {
                apply(item);
            }
        }
        return Task.CompletedTask;
    }
}
