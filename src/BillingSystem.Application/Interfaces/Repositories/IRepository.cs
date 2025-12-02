namespace BillingSystem.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for basic CRUD operations
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(int id);
}
