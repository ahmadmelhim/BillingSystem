namespace BillingSystem.Core.DTOs;

/// <summary>
/// DTO for system-wide overview statistics (Admin only)
/// </summary>
public class SystemOverviewDto
{
    // Total Counts
    public int TotalUsers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalInvoicesAmount { get; set; }
    public int TotalPayments { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    // Invoice Status Breakdown
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal OverdueAmount { get; set; }

    // Growth Rates (compared to previous period)
    public double UserGrowthRate { get; set; }
    public double CustomerGrowthRate { get; set; }
    public double RevenueGrowthRate { get; set; }

    // Calculated Metrics
    public decimal AverageInvoiceValue => TotalInvoices > 0 
        ? TotalInvoicesAmount / TotalInvoices 
        : 0;

    public double CollectionRate => TotalInvoicesAmount > 0 
        ? (double)(TotalPaymentsAmount / TotalInvoicesAmount * 100) 
        : 0;

    // Role Distribution
    public int AdminCount { get; set; }
    public int AccountantCount { get; set; }
    public int CustomerCount { get; set; }
}
