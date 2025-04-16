using System.Linq.Expressions;

namespace CMS.API.Repository.IRepository;

public interface IRepository<T> where T : class
{

    Task<T> AddAsync(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false);

    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);

    void Remove(T entity);
    void RemoveRange(IList<T> entity);
    Task<int> CountAsync(Expression<Func<T, bool>> filter);
}
