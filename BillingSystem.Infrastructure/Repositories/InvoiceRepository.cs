using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search, string? status)
    {
        var query = _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId);

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
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var now = DateTime.UtcNow;
        var prefix = $"INV-{now:yyyyMM}-";
        
        var lastInvoice = await _dbSet
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
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Include(i => i.Customer)
            .Where(i => i.Status == "Pending" && i.DueDate < today)
            .ToListAsync();
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        invoice.CreatedAt = DateTime.UtcNow;
        await AddAsync(invoice);
        return invoice;
    }

    public async Task UpdateInvoiceAsync(Invoice invoice)
    {
        var existing = await _dbSet
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
        _context.InvoiceItems.RemoveRange(existing.Items);
        existing.Items = invoice.Items;

        await UpdateAsync(existing);
    }

    public async Task<bool> HasPaymentsAsync(int invoiceId)
    {
        return await _context.Payments.AnyAsync(p => p.InvoiceId == invoiceId);
    }

    public async Task<Invoice?> GetByIdAsync(int id, int userId)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}

