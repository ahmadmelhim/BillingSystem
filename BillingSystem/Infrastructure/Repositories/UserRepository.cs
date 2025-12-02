using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var context = await CreateContextAsync();
        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        using var context = await CreateContextAsync();
        var query = context.Users.Where(u => u.Email == email);
        
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        using var context = await CreateContextAsync();
        return await context.Users
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search = null, string? role = null)
    {
        using var context = await CreateContextAsync();
        var query = context.Users.AsQueryable().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => 
                u.FullName.Contains(search) || 
                u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role) && role != "All")
        {
            query = query.Where(u => u.Role == role);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<User> CreateUserAsync(User user, string passwordHash)
    {
        var exists = await EmailExistsAsync(user.Email);
        if (exists)
            throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");

        user.PasswordHash = passwordHash;
        user.CreatedAt = DateTime.UtcNow;

        return await AddAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        using var context = await CreateContextAsync();
        var existing = await context.Users.FindAsync(user.Id);
        
        if (existing == null)
            throw new KeyNotFoundException("المستخدم غير موجود");

        // Check email uniqueness if changed
        if (existing.Email != user.Email)
        {
            // We need to check existence in a separate context or query, 
            // but since we are inside UpdateUserAsync which has its own context,
            // we can query the same context if we haven't tracked conflicting entities yet.
            // However, EmailExistsAsync creates its own context.
            // To be safe and efficient, let's query directly here.
            var emailExists = await context.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id);
            if (emailExists)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;

        context.Users.Update(existing);
        await context.SaveChangesAsync();
    }
}

