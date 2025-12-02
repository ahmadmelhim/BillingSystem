namespace BillingSystem.Core.DTOs;

/// <summary>
/// DTO for recent system activities (Admin dashboard)
/// </summary>
public class ActivityLogDto
{
    public string ActivityType { get; set; } = string.Empty; // "UserRegistered", "InvoiceCreated", "PaymentReceived", etc.
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RelatedEmail { get; set; }
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty; // MudBlazor icon name
    public string Color { get; set; } = "Default"; // MudBlazor color name
    public decimal? Amount { get; set; } // For financial activities
    
    // Alias for Razor compatibility
    public string User => UserName;
}
