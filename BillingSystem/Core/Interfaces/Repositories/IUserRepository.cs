using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    
    Task<IEnumerable<User>> GetByRoleAsync(string role);
    
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search = null, string? role = null);
    
    Task<User> CreateUserAsync(User user, string passwordHash);
    
    Task UpdateUserAsync(User user);
}
