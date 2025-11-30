using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext db, 
        ICurrentUserService currentUserService,
        ILogger<InvoiceService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? search,
        string? status,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return (Array.Empty<Invoice>(), 0);

        var userId = currentUserId.Value;
        
        // Build query with data isolation
        var query = _db.Invoices
            .AsNoTracking() // ✅ Performance: No change tracking needed for read-only query
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                (i.Customer != null && i.Customer.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            var today = DateTime.Today;
            
            if (status == "Overdue")
            {
                // ? ???????? ????????: ??? DueDate ?? ?????? ????? ?????? ?? ?????
                query = query.Where(i => 
                    i.DueDate.HasValue && 
                    i.DueDate.Value.Date < today &&
                    i.Status != "Paid" &&
                    i.Status != "Cancelled");
            }
            else if (status == "Pending")
            {
                // ? ???????? ??? ????????: Status = "Pending" ? (?? ???? DueDate ?? DueDate >= ?????)
                query = query.Where(i => 
                    i.Status == "Pending" &&
                    (!i.DueDate.HasValue || i.DueDate.Value.Date >= today));
            }
            else
            {
                // ???? ??????? (Paid, Cancelled, ???)
                query = query.Where(i => i.Status == status);
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.DateIssued >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.DateIssued <= toDate.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.DateIssued)
            .ThenByDescending(i => i.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return null;

        return await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Where(i => i.UserId == currentUserId.Value)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<string> GenerateNextInvoiceNumberAsync(DateTime dateIssued)
    {
        var prefix = $"INV-{dateIssued:yyyyMM}-";

        var lastInvoice = await _db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        var nextNumber = 1;

        if (lastInvoice != null)
        {
            var suffix = lastInvoice.InvoiceNumber.Substring(prefix.Length);
            if (int.TryParse(suffix, out var last))
                nextNumber = last + 1;
        }

        return prefix + nextNumber.ToString("D3");
    }

    public async Task<Invoice> CreateAsync(Invoice invoice, List<InvoiceItem> items)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to create invoice without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            invoice.InvoiceNumber = await GenerateNextInvoiceNumberAsync(invoice.DateIssued);

        foreach (var item in items)
            item.TotalPrice = item.Quantity * item.UnitPrice;

        invoice.TotalAmount = items.Sum(i => i.TotalPrice);
        invoice.CreatedAt = DateTime.UtcNow;
        invoice.UserId = currentUserId.Value;
        invoice.Items = items;

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created invoice {InvoiceNumber} for user {UserId} with total {Total}", 
            invoice.InvoiceNumber, currentUserId.Value, invoice.TotalAmount);

        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, List<InvoiceItem> items)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to update invoice without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var existing = await _db.Invoices
            .Include(i => i.Items)
            .Where(i => i.UserId == currentUserId.Value)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (existing == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoice.Id, currentUserId.Value);
            throw new KeyNotFoundException("Invoice not found.");
        }

        existing.CustomerId = invoice.CustomerId;
        existing.DateIssued = invoice.DateIssued;
        existing.DueDate = invoice.DueDate;
        existing.Status = invoice.Status;

        _db.InvoiceItems.RemoveRange(existing.Items);
        existing.Items.Clear();

        foreach (var item in items)
        {
            item.Id = 0;
            item.InvoiceId = existing.Id;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            existing.Items.Add(item);
        }

        existing.TotalAmount = existing.Items.Sum(i => i.TotalPrice);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated invoice {InvoiceNumber} (ID: {InvoiceId}) for user {UserId}", 
            existing.InvoiceNumber, existing.Id, currentUserId.Value);
    }

    public async Task DeleteAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to delete invoice without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        // تحقق من عدم وجود مدفوعات على الفاتورة
        var hasPayments = await _db.Payments
            .AnyAsync(p => p.InvoiceId == id);

        if (hasPayments)
        {
            _logger.LogWarning("Attempt to delete invoice {InvoiceId} with existing payments", id);
            throw new InvalidOperationException("لا يمكن حذف الفاتورة لوجود مدفوعات.");
        }

        var existing = await _db.Invoices
            .Include(i => i.Items)
            .Where(i => i.UserId == currentUserId.Value)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (existing == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for deletion", id);
            return;
        }

        _db.InvoiceItems.RemoveRange(existing.Items);
        _db.Invoices.Remove(existing);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted invoice {InvoiceNumber} (ID: {InvoiceId}) for user {UserId}", 
            existing.InvoiceNumber, id, currentUserId.Value);
    }
}

