using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface ICustomerService
{
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search);

    Task<Customer?> GetByIdAsync(int id);

    Task<Customer> CreateAsync(Customer customer);

    Task UpdateAsync(Customer customer);

    Task DeleteAsync(int id);
}
