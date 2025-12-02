using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;

namespace BillingSystem.Infrastructure.Services.Business;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUserService _currentUserService;

    public ReportService(
        IReportRepository reportRepository, 
        ICurrentUserService currentUserService)
    {
        _reportRepository = reportRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CustomerReportDto>> GetCustomerSummaryAsync(
        string? search,
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<CustomerReportDto>();

        return await _reportRepository.GetCustomerSummaryAsync(currentUserId.Value, search, from, to);
    }

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return new InvoiceSummaryDto();

        return await _reportRepository.GetInvoiceSummaryAsync(currentUserId.Value, from, to);
    }

    public async Task<IReadOnlyList<PaymentsPerPeriodDto>> GetPaymentsPerDayAsync(
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<PaymentsPerPeriodDto>();

        return await _reportRepository.GetPaymentsPerDayAsync(currentUserId.Value, from, to);
    }

    public async Task<IReadOnlyList<PaymentReportDto>> GetPaymentsAsync(string? search, DateTime? from, DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<PaymentReportDto>();

        return await _reportRepository.GetPaymentsAsync(currentUserId.Value, search, from, to);
    }

    public async Task<DashboardDto> GetDashboardSummaryAsync()
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return new DashboardDto();

        return await _reportRepository.GetDashboardSummaryAsync(currentUserId.Value);
    }
}
