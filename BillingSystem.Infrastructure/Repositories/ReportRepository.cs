using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _db;

    public ReportRepository(ApplicationDbContext context)
    {
        _db = context;
    }

    public async Task<IReadOnlyList<CustomerReportDto>> GetCustomerSummaryAsync(
        int userId, string? search, DateTime? from, DateTime? to)
    {
        var query = _db.Customers
            .Where(c => c.UserId == userId)
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
                Email = c.Email,
                Phone = c.Phone,
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

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(
        int userId, DateTime? from, DateTime? to)
    {
        var invoices = _db.Invoices
            .Where(i => i.UserId == userId)
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

    public async Task<IReadOnlyList<PaymentsPerPeriodDto>> GetPaymentsPerDayAsync(
        int userId, DateTime? from, DateTime? to)
    {
        var payments = _db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.UserId == userId)
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

    public async Task<IReadOnlyList<PaymentReportDto>> GetPaymentsAsync(
        int userId, string? search, DateTime? from, DateTime? to)
    {
        var query = _db.Payments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Customer)
            .Where(p => p.Invoice.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(p => p.Date.Date >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(p => p.Date.Date <= to.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p =>
                (p.Invoice != null && p.Invoice.InvoiceNumber.Contains(search)) ||
                (p.Invoice != null && p.Invoice.Customer != null && p.Invoice.Customer.Name.Contains(search)) ||
                (p.Method != null && p.Method.Contains(search)));
        }

        var result = await query
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Id)
            .Select(p => new PaymentReportDto
            {
                Id = p.Id,
                Date = p.Date,
                Amount = p.Amount,
                Method = p.Method ?? string.Empty,
                Note = p.Notes ?? string.Empty,
                InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : string.Empty,
                CustomerName = (p.Invoice != null && p.Invoice.Customer != null) ? p.Invoice.Customer.Name : string.Empty
            })
            .ToListAsync();

        return result;
    }

    public async Task<DashboardDto> GetDashboardSummaryAsync(int userId)
    {
        var today = DateTime.Today;

        // Fetch Data
        var invoices = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var customersCount = await _db.Customers
            .Where(c => c.UserId == userId)
            .CountAsync();

        var payments = await _db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.UserId == userId)
            .ToListAsync();

        // Calculate Stats
        var dto = new DashboardDto
        {
            TotalInvoices = invoices.Count,
            TotalAmount = invoices.Sum(i => i.TotalAmount),
            TotalCustomers = customersCount
        };

        var paid = invoices.Where(i => i.Status == "Paid").ToList();
        dto.PaidInvoices = paid.Count;
        dto.PaidAmount = paid.Sum(i => i.TotalAmount);

        var pending = invoices
            .Where(i => i.Status == "Pending" && (!i.DueDate.HasValue || i.DueDate.Value.Date >= today))
            .ToList();
        dto.PendingInvoices = pending.Count;
        dto.PendingAmount = pending.Sum(i => i.TotalAmount);

        var overdue = invoices
            .Where(i => i.DueDate.HasValue && i.DueDate.Value.Date < today && i.Status != "Paid" && i.Status != "Cancelled")
            .ToList();
        dto.OverdueInvoices = overdue.Count;
        dto.OverdueAmount = overdue.Sum(i => i.TotalAmount);

        if (payments.Any())
        {
            dto.TotalPayments = payments.Count;
            dto.TotalPaymentsAmount = payments.Sum(p => p.Amount);
        }
        else
        {
            dto.TotalPayments = paid.Count;
            dto.TotalPaymentsAmount = dto.PaidAmount;
        }

        // Charts: Status Distribution
        dto.InvoiceStatusDistribution = new List<ChartDataDto>
        {
            new() { Label = "Paid", Value = dto.PaidInvoices },
            new() { Label = "Pending", Value = dto.PendingInvoices },
            new() { Label = "Overdue", Value = dto.OverdueInvoices }
        };

        // Charts: Payments per Month
        var paymentsByMonth = payments
            .GroupBy(p => new { p.Date.Year, p.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Take(12)
            .ToList();

        dto.PaymentsByMonth = paymentsByMonth.Select(x => new ChartDataDto
        {
            Label = $"{x.Month}/{x.Year}",
            Value = (double)x.Total
        }).ToList();

        // Charts: Top 5 Customers
        var topCustomers = invoices
            .GroupBy(i => i.Customer?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Total = g.Sum(i => i.TotalAmount) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToList();

        dto.TopCustomers = topCustomers.Select(x => new ChartDataDto
        {
            Label = x.Name,
            Value = (double)x.Total
        }).ToList();

        return dto;
    }
}
