using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services.Business;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public AuditService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task LogAsync(string userId, string userName, string action, string entityName, string entityId, string details, string ipAddress = "")
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };
            
            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Fail silently to not disrupt main application flow
            // In production, consider logging to a separate error tracking system
        }
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetLogsByUserAsync(string userId, int count = 50)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityName, string entityId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.EntityName == entityName && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }
}
