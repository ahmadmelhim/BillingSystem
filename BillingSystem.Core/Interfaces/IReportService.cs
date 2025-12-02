using BillingSystem.Core.DTOs;

namespace BillingSystem.Core.Interfaces;

public interface IReportService
{
    Task<IReadOnlyList<CustomerReportDto>> GetCustomerSummaryAsync(
        string? search,
        DateTime? from,
        DateTime? to);

    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(
        DateTime? from,
        DateTime? to);

    Task<IReadOnlyList<PaymentsPerPeriodDto>> GetPaymentsPerDayAsync(
        DateTime? from,
        DateTime? to);

    Task<IReadOnlyList<PaymentReportDto>> GetPaymentsAsync(
        string? search,
        DateTime? from,
        DateTime? to);

    Task<DashboardDto> GetDashboardSummaryAsync();
}
