using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId)
    {
        using var context = await CreateContextAsync();
        return await context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> GetTotalByInvoiceIdAsync(int invoiceId)
    {
        using var context = await CreateContextAsync();
        return await context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => p.Amount);
    }

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search)
    {
        using var context = await CreateContextAsync();
        var query = context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Customer)
            .Where(p => p.Invoice.UserId == userId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p =>
                (p.Invoice != null && p.Invoice.InvoiceNumber.Contains(search)) ||
                (p.Invoice != null && p.Invoice.Customer != null && p.Invoice.Customer.Name.Contains(search)) ||
                (p.Method != null && p.Method.Contains(search)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.Date)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Payment?> GetByIdAsync(int id, int userId)
    {
        using var context = await CreateContextAsync();
        return await context.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        payment.Date = DateTime.UtcNow;
        return await AddAsync(payment);
    }
}

