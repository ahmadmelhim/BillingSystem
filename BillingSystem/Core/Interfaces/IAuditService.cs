using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(string userId, string userName, string action, string entityName, string entityId, string details, string ipAddress = "");
    Task<List<AuditLog>> GetRecentLogsAsync(int count = 100);
    Task<List<AuditLog>> GetLogsByUserAsync(string userId, int count = 50);
    Task<List<AuditLog>> GetLogsByEntityAsync(string entityName, string entityId);
}
