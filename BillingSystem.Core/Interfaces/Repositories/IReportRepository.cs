using BillingSystem.Core.DTOs;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IReportRepository
{
    Task<IReadOnlyList<CustomerReportDto>> GetCustomerSummaryAsync(
        int userId, string? search, DateTime? from, DateTime? to);
    
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(
        int userId, DateTime? from, DateTime? to);
    
    Task<IReadOnlyList<PaymentsPerPeriodDto>> GetPaymentsPerDayAsync(
        int userId, DateTime? from, DateTime? to);
    
    Task<IReadOnlyList<PaymentReportDto>> GetPaymentsAsync(
        int userId, string? search, DateTime? from, DateTime? to);
    
    Task<DashboardDto> GetDashboardSummaryAsync(int userId);
}
