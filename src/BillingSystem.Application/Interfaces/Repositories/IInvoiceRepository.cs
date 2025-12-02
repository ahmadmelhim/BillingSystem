using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int userId, int pageIndex, int pageSize, string? search, string? status);
    
    Task<Invoice?> GetByIdWithDetailsAsync(int id, int userId);
    
    Task<string> GenerateInvoiceNumberAsync();
    
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
}
