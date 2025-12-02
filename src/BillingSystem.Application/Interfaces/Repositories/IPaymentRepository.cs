using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId);
    
    Task<decimal> GetTotalByInvoiceIdAsync(int invoiceId);
}
