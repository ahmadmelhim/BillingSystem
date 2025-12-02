using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for basic CRUD operations using IDbContextFactory for Blazor Server thread safety
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public Repository(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    protected async Task<ApplicationDbContext> CreateContextAsync()
    {
        return await _contextFactory.CreateDbContextAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        using var context = await CreateContextAsync();
        return await context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var context = await CreateContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        using var context = await CreateContextAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        using var context = await CreateContextAsync();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        using var context = await CreateContextAsync();
        var entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public virtual async Task<int> CountAsync()
    {
        using var context = await CreateContextAsync();
        return await context.Set<T>().CountAsync();
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        using var context = await CreateContextAsync();
        return await context.Set<T>().FindAsync(id) != null;
    }
}
