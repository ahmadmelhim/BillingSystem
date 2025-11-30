using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext db, 
        ICurrentUserService currentUserService,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // 📌 جلب المدفوعات المرتبطة بفاتورة معينة
    public async Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(int invoiceId)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return Array.Empty<Payment>();

        // تحقق من أن الفاتورة تابعة للمستخدم الحالي
        var invoiceExists = await _db.Invoices
            .AnyAsync(i => i.Id == invoiceId && i.UserId == currentUserId.Value);

        if (!invoiceExists)
            return Array.Empty<Payment>();

        return await _db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    // 📝 إنشاء دفعة جديدة مع تحقق كامل من الصلاحيات
    public async Task<Payment> CreateAsync(Payment payment)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to create payment without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var invoice = await _db.Invoices
            .Include(i => i.Payments)
            .Where(i => i.UserId == currentUserId.Value)
            .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId);

        if (invoice == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for payment creation", payment.InvoiceId);
            throw new KeyNotFoundException("الفاتورة غير موجودة.");
        }

        if (payment.Amount <= 0)
        {
            _logger.LogWarning("Invalid payment amount: {Amount}", payment.Amount);
            throw new InvalidOperationException("قيمة الدفعة يجب أن تكون أكبر من صفر.");
        }

        var alreadyPaid = invoice.Payments.Sum(p => p.Amount);
        var newTotalPaid = alreadyPaid + payment.Amount;

        if (newTotalPaid > invoice.TotalAmount)
        {
            _logger.LogWarning("Payment amount {Amount} exceeds remaining balance for invoice {InvoiceId}", 
                payment.Amount, invoice.Id);
            throw new InvalidOperationException("لا يمكن أن تتجاوز الدفعة المبلغ المتبقي للفاتورة.");
        }

        if (payment.Date == default)
            payment.Date = DateTime.Today;

        payment.CreatedAt = DateTime.UtcNow;
        _db.Payments.Add(payment);

        // تحديث حالة الفاتورة تلقائياً
        if (newTotalPaid == invoice.TotalAmount)
        {
            invoice.Status = "Paid";
        }
        else if (newTotalPaid > 0 && newTotalPaid < invoice.TotalAmount)
        {
            invoice.Status = "Pending";
        }
        else
        {
            invoice.Status = "Pending";
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Created payment {PaymentId} for invoice {InvoiceNumber} (Amount: {Amount})", 
            payment.Id, invoice.InvoiceNumber, payment.Amount);

        return payment;
    }

    // 🗑 حذف دفعة بتحقق كامل من الصلاحيات
    public async Task DeleteAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to delete payment without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var payment = await _db.Payments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Payments)
            .Where(p => p.Invoice.UserId == currentUserId.Value)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found for deletion", id);
            return;
        }

        var invoice = payment.Invoice;
        var deletedAmount = payment.Amount;

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();

        // إعادة حساب حالة الفاتورة
        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id)
            .SumAsync(p => p.Amount);

        if (totalPaid == 0)
        {
            invoice.Status = "Pending";
        }
        else if (totalPaid >= invoice.TotalAmount)
        {
            invoice.Status = "Paid";
        }
        else
        {
            invoice.Status = "Pending";
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted payment {PaymentId} (Amount: {Amount}) from invoice {InvoiceNumber}", 
            id, deletedAmount, invoice.InvoiceNumber);
    }
}

