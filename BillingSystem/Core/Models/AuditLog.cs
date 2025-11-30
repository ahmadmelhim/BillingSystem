using System;

namespace BillingSystem.Core.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // e.g., "Login", "Delete Invoice", "Update Customer"
    public string EntityName { get; set; } = string.Empty; // e.g., "Invoice", "User", "Customer"
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty; // JSON or description
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
