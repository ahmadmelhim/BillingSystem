using System.Net.Http.Json;
using BillingSystem.Core.Models;
using BillingSystem.Core.Interfaces;

namespace BillingSystem.Web.Services.ApiClients;

public interface IInvoiceApiService
{
    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Invoice?> GetByIdAsync(int id);
    Task<string> GenerateNextInvoiceNumberAsync(DateTime dateIssued);
    Task<Invoice> CreateAsync(Invoice invoice, List<InvoiceItem> items);
    Task UpdateAsync(Invoice invoice, List<InvoiceItem> items);
    Task DeleteAsync(int id);
    Task<byte[]?> GeneratePdfAsync(int id);
}

public class InvoiceApiService : IInvoiceApiService, IInvoiceService, IPdfService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceApiService> _logger;

    public InvoiceApiService(HttpClient httpClient, ILogger<InvoiceApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var url = $"api/invoices?pageIndex={pageIndex}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(status))
                url += $"&status={Uri.EscapeDataString(status)}";
            if (fromDate.HasValue)
                url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            if (toDate.HasValue)
                url += $"&toDate={toDate.Value:yyyy-MM-dd}";

            var response = await _httpClient.GetFromJsonAsync<PagedResult<Invoice>>(url);
            
            return (response?.Items ?? Array.Empty<Invoice>(), response?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged invoices");
            return (Array.Empty<Invoice>(), 0);
        }
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Invoice>($"api/invoices/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {Id}", id);
            return null;
        }
    }

    public async Task<string> GenerateNextInvoiceNumberAsync(DateTime dateIssued)
    {
        try
        {
            return await _httpClient.GetStringAsync($"api/invoices/next-number?date={dateIssued:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice number");
            return "INV-0000";
        }
    }

    public async Task<Invoice> CreateAsync(Invoice invoice, List<InvoiceItem> items)
    {
        try
        {
            invoice.Items = items; // Attach items
            var response = await _httpClient.PostAsJsonAsync("api/invoices", invoice);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Invoice>()
                ?? throw new InvalidOperationException("Failed to deserialize invoice");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            throw;
        }
    }

    public async Task UpdateAsync(Invoice invoice, List<InvoiceItem> items)
    {
        try
        {
            invoice.Items = items; // Attach items
            var response = await _httpClient.PutAsJsonAsync($"api/invoices/{invoice.Id}", invoice);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {Id}", invoice.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/invoices/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {Id}", id);
            throw;
        }
    }

    async Task<byte[]> IPdfService.GenerateInvoicePdfAsync(int invoiceId)
    {
        return await GeneratePdfAsync(invoiceId) ?? Array.Empty<byte>();
    }

    public async Task<byte[]?> GeneratePdfAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/invoices/{id}/pdf");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for invoice {Id}", id);
            return null;
        }
    }
}
