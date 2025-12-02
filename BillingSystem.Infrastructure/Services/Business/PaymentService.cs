using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ICurrentUserService currentUserService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
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
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, currentUserId.Value);

        if (invoice == null)
            return Array.Empty<Payment>();

        var payments = await _paymentRepository.GetByInvoiceIdAsync(invoiceId);
        return payments.ToList();
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

        var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(payment.InvoiceId, currentUserId.Value);

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

        var alreadyPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0;
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
        
        var created = await _paymentRepository.CreatePaymentAsync(payment);

        // تحديث حالة الفاتورة تلقائياً
        if (newTotalPaid >= invoice.TotalAmount)
        {
            invoice.Status = "Paid";
        }
        else if (newTotalPaid > 0)
        {
            invoice.Status = "Pending";
        }

        await _invoiceRepository.UpdateInvoiceAsync(invoice);

        _logger.LogInformation("Created payment {PaymentId} for invoice {InvoiceNumber} (Amount: {Amount})", 
            created.Id, invoice.InvoiceNumber, payment.Amount);

        return created;
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

        var payment = await _paymentRepository.GetByIdAsync(id, currentUserId.Value);

        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found for deletion", id);
            return;
        }

        var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(payment.InvoiceId, currentUserId.Value);
        
        if (invoice == null)
        {
            _logger.LogWarning("Invoice for payment {PaymentId} not found", id);
            return;
        }

        var deletedAmount = payment.Amount;

        await _paymentRepository.DeleteAsync(id);

        // إعادة حساب حالة الفاتورة
        var remainingPayments = await _paymentRepository.GetByInvoiceIdAsync(invoice.Id);
        var totalPaid = remainingPayments.Sum(p => p.Amount);

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

        await _invoiceRepository.UpdateInvoiceAsync(invoice);

        _logger.LogInformation("Deleted payment {PaymentId} (Amount: {Amount}) from invoice {InvoiceNumber}", 
            id, deletedAmount, invoice.InvoiceNumber);
    }
}
