using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId);
    
    Task<decimal> GetTotalByInvoiceIdAsync(int invoiceId);
    
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search);
    
    Task<Payment?> GetByIdAsync(int id, int userId);
    
    Task<Payment> CreatePaymentAsync(Payment payment);
}
