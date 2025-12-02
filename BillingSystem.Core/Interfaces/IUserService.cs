using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface IUserService
{
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null, string? role = null);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}
