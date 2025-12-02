using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    
    Task<IEnumerable<User>> GetByRoleAsync(string role);
}
