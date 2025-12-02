using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search);
    
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}
