using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services.Business;

public interface IUserService
{
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null, string? role = null);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, 
        int pageSize, 
        string? search = null,
        string? role = null)
    {
        var query = _db.Users.AsQueryable();

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

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == user.Email);
        if (exists)
            throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing == null)
            throw new KeyNotFoundException("المستخدم غير موجود");

        // Check email uniqueness if changed
        if (existing.Email != user.Email)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id);
            if (emailExists)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل");
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}

