using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var query = _dbSet.Where(u => u.Email == email);
        
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        return await _dbSet
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search = null, string? role = null)
    {
        var query = _dbSet.AsQueryable();

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

        await AddAsync(user);
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        var existing = await GetByIdAsync(user.Id);
        if (existing == null)
            throw new KeyNotFoundException("المستخدم غير موجود");

        // Check email uniqueness if changed
        if (existing.Email != user.Email)
        {
            var emailExists = await EmailExistsAsync(user.Email, user.Id);
            if (emailExists)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;

        await UpdateAsync(existing);
    }
}

