namespace BillingSystem.Core.DTOs;

public class DashboardDto
{
    // Invoice Stats
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    
    public int PaidInvoices { get; set; }
    public decimal PaidAmount { get; set; }
    
    public int PendingInvoices { get; set; }
    public decimal PendingAmount { get; set; }
    
    public int OverdueInvoices { get; set; }
    public decimal OverdueAmount { get; set; }

    // Other Stats
    public int TotalCustomers { get; set; }
    public int TotalPayments { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    // Charts Data
    public List<ChartDataDto> PaymentsByMonth { get; set; } = new();
    public List<ChartDataDto> TopCustomers { get; set; } = new();
    public List<ChartDataDto> InvoiceStatusDistribution { get; set; } = new();
}

public class ChartDataDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}
