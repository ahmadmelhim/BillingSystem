using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search, string? status);
    
    Task<Invoice?> GetByIdWithDetailsAsync(int id, int userId);
    Task<Invoice?> GetByIdWithDetailsSystemAsync(int id);
    
    Task<string> GenerateInvoiceNumberAsync();
    
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
    
    Task<Invoice> CreateInvoiceAsync(Invoice invoice);
    
    Task UpdateInvoiceAsync(Invoice invoice);
    
    Task<bool> HasPaymentsAsync(int invoiceId);
    
    Task<Invoice?> GetByIdAsync(int id, int userId);
}
