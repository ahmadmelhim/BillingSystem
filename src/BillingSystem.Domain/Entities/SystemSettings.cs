namespace BillingSystem.Core.Models;

public class SystemSettings
{
    public string CompanyName { get; set; } = "Billing System";
    public string CurrencySymbol { get; set; } = "₪";
    public decimal DefaultTaxRate { get; set; } = 15.0m; // Percentage
    public bool MaintenanceMode { get; set; } = false;
    public bool AllowNewRegistrations { get; set; } = true;
}
