using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services.Business;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public ReportService(ApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    // 1) Customer list with total invoices and payments
    public async Task<IReadOnlyList<CustomerReportDto>> GetCustomerSummaryAsync(
        string? search,
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<CustomerReportDto>();

        var query = _db.Customers
            .Where(c => c.UserId == currentUserId.Value)
            .Include(c => c.Invoices)
                .ThenInclude(i => i.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        if (from.HasValue)
        {
            query = query.Where(c =>
                c.Invoices.Any(i => i.DateIssued >= from.Value));
        }

        if (to.HasValue)
        {
            query = query.Where(c =>
                c.Invoices.Any(i => i.DateIssued <= to.Value));
        }

        var list = await query
            .Select(c => new CustomerReportDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                InvoiceCount = c.Invoices.Count,
                TotalInvoiced = c.Invoices.Sum(i => (decimal?)i.TotalAmount) ?? 0,
                TotalPaid = c.Invoices
                    .SelectMany(i => i.Payments)
                    .Sum(p => (decimal?)p.Amount) ?? 0
            })
            .OrderByDescending(x => x.TotalInvoiced)
            .ToListAsync();

        return list;
    }

    // 2) Invoice summary: Paid, Pending, Overdue
    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return new InvoiceSummaryDto();

        var invoices = _db.Invoices
            .Where(i => i.UserId == currentUserId.Value)
            .AsQueryable();

        if (from.HasValue)
            invoices = invoices.Where(i => i.DateIssued >= from.Value);

        if (to.HasValue)
            invoices = invoices.Where(i => i.DateIssued <= to.Value);

        var today = DateTime.Today;

        var paid = await invoices
            .Where(i => i.Status == "Paid")
            .ToListAsync();

        var pending = await invoices
            .Where(i => i.Status == "Pending" &&
                        (i.DueDate == null || i.DueDate >= today))
            .ToListAsync();

        var overdue = await invoices
            .Where(i => i.Status != "Paid" &&
                        i.DueDate != null &&
                        i.DueDate < today)
            .ToListAsync();

        return new InvoiceSummaryDto
        {
            PaidCount = paid.Count,
            PendingCount = pending.Count,
            OverdueCount = overdue.Count,

            PaidTotal = paid.Sum(i => i.TotalAmount),
            PendingTotal = pending.Sum(i => i.TotalAmount),
            OverdueTotal = overdue.Sum(i => i.TotalAmount)
        };
    }

    // 3) Payments received per day
    public async Task<IReadOnlyList<PaymentsPerPeriodDto>> GetPaymentsPerDayAsync(
        DateTime? from,
        DateTime? to)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<PaymentsPerPeriodDto>();

        var payments = _db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.UserId == currentUserId.Value)
            .AsQueryable();

        if (from.HasValue)
            payments = payments.Where(p => p.Date >= from.Value.Date);

        if (to.HasValue)
            payments = payments.Where(p => p.Date <= to.Value.Date);

        var result = await payments
            .GroupBy(p => p.Date.Date)
            .Select(g => new PaymentsPerPeriodDto
            {
                Period = g.Key,
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderBy(x => x.Period)
            .ToListAsync();

        return result;
    }
}

