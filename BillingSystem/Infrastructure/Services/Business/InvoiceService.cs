using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        ICurrentUserService currentUserService,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
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

        // استخدام Repository  - ملاحظة: الفلترة بالتواريخ غير مدعومة في Repository الحالي
        // يمكن تحديث Repository لاحقاً لدعمها أو نتعامل مع النتائج
        return await _invoiceRepository.GetPagedAsync(currentUserId.Value, pageIndex, pageSize, search, status);
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return null;

        return await _invoiceRepository.GetByIdAsync(id, currentUserId.Value);
    }

    public async Task<string> GenerateNextInvoiceNumberAsync(DateTime dateIssued)
    {
        return await _invoiceRepository.GenerateInvoiceNumberAsync();
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

        var created = await _invoiceRepository.CreateInvoiceAsync(invoice);

        _logger.LogInformation("Created invoice {InvoiceNumber} for user {UserId} with total {Total}", 
            invoice.InvoiceNumber, currentUserId.Value, invoice.TotalAmount);

        return created;
    }

    public async Task UpdateAsync(Invoice invoice, List<InvoiceItem> items)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to update invoice without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var existing = await _invoiceRepository.GetByIdAsync(invoice.Id, currentUserId.Value);

        if (existing == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoice.Id, currentUserId.Value);
            throw new KeyNotFoundException("Invoice not found.");
        }

        existing.CustomerId = invoice.CustomerId;
        existing.DateIssued = invoice.DateIssued;
        existing.DueDate = invoice.DueDate;
        existing.Status = invoice.Status;

        // Update items
        foreach (var item in items)
        {
            item.InvoiceId = existing.Id;
            item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        existing.Items = items;
        existing.TotalAmount = items.Sum(i => i.TotalPrice);

        await _invoiceRepository.UpdateInvoiceAsync(existing);

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
        var hasPayments = await _invoiceRepository.HasPaymentsAsync(id);

        if (hasPayments)
        {
            _logger.LogWarning("Attempt to delete invoice {InvoiceId} with existing payments", id);
            throw new InvalidOperationException("لا يمكن حذف الفاتورة لوجود مدفوعات.");
        }

        var existing = await _invoiceRepository.GetByIdAsync(id, currentUserId.Value);

        if (existing == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for deletion", id);
            return;
        }

        await _invoiceRepository.DeleteAsync(existing.Id);

        _logger.LogInformation("Deleted invoice {InvoiceNumber} (ID: {InvoiceId}) for user {UserId}", 
            existing.InvoiceNumber, id, currentUserId.Value);
    }
}
