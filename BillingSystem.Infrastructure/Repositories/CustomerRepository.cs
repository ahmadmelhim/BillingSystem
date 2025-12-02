using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search)
    {
        var query = _dbSet.Where(c => c.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Email == email);
        
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
