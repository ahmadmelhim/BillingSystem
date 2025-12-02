using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search, string? status)
    {
        using var context = await CreateContextAsync();
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                i.Customer.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(i => i.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.DateIssued)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Invoice?> GetByIdWithDetailsAsync(int id, int userId)
    {
        using var context = await CreateContextAsync();
        return await context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice?> GetByIdWithDetailsSystemAsync(int id)
    {
        using var context = await CreateContextAsync();
        return await context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        using var context = await CreateContextAsync();
        var now = DateTime.UtcNow;
        var prefix = $"INV-{now:yyyyMM}-";
        
        var lastInvoice = await context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var lastNumberStr = lastInvoice.InvoiceNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D3}";
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
    {
        using var context = await CreateContextAsync();
        var today = DateTime.UtcNow.Date;
        return await context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.Status == "Pending" && i.DueDate < today)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        invoice.CreatedAt = DateTime.UtcNow;
        return await AddAsync(invoice);
    }

    public async Task UpdateInvoiceAsync(Invoice invoice)
    {
        using var context = await CreateContextAsync();
        var existing = await context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (existing == null)
            throw new KeyNotFoundException("الفاتورة غير موجودة");

        // Update invoice properties
        existing.CustomerId = invoice.CustomerId;
        existing.DateIssued = invoice.DateIssued;
        existing.DueDate = invoice.DueDate;
        existing.Status = invoice.Status;
        existing.TotalAmount = invoice.TotalAmount;

        // Update items
        context.InvoiceItems.RemoveRange(existing.Items);
        existing.Items = invoice.Items;

        context.Invoices.Update(existing);
        await context.SaveChangesAsync();
    }

    public async Task<bool> HasPaymentsAsync(int invoiceId)
    {
        using var context = await CreateContextAsync();
        return await context.Payments.AnyAsync(p => p.InvoiceId == invoiceId);
    }

    public async Task<Invoice?> GetByIdAsync(int id, int userId)
    {
        using var context = await CreateContextAsync();
        return await context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}

