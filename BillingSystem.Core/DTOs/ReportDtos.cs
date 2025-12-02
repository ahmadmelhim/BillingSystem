namespace BillingSystem.Core.DTOs;

public class CustomerReportDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public int InvoiceCount { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding => TotalInvoiced - TotalPaid;
}

public class InvoiceSummaryDto
{
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int OverdueCount { get; set; }

    public decimal PaidTotal { get; set; }
    public decimal PendingTotal { get; set; }
    public decimal OverdueTotal { get; set; }
}

public class PaymentsPerPeriodDto
{
    public DateTime Period { get; set; }   // اليوم
    public decimal TotalAmount { get; set; }
}
