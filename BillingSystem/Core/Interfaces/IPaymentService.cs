using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(int invoiceId);

    Task<Payment> CreateAsync(Payment payment);

    Task DeleteAsync(int id);
}
