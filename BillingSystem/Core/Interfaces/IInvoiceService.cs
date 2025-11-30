using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface IInvoiceService
{
    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? search,
        string? status,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<Invoice?> GetByIdAsync(int id);

    Task<string> GenerateNextInvoiceNumberAsync(DateTime dateIssued);

    Task<Invoice> CreateAsync(Invoice invoice, List<InvoiceItem> items);

    Task UpdateAsync(Invoice invoice, List<InvoiceItem> items);

    Task DeleteAsync(int id);
}
