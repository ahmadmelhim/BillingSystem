using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => p.Amount);
    }

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search)
    {
        var query = _dbSet
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Customer)
            .Where(p => p.Invoice.UserId == userId);

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
        return await _dbSet
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.UserId == userId)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        payment.Date = DateTime.UtcNow;
        await AddAsync(payment);
        return payment;
    }
}

