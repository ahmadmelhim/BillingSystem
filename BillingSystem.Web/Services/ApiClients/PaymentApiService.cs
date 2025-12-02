using System.Net.Http.Json;
using BillingSystem.Core.Models;

namespace BillingSystem.Web.Services.ApiClients;

public interface IPaymentApiService
{
    Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(int invoiceId);
    Task<Payment?> CreateAsync(Payment payment);
    Task<bool> DeleteAsync(int id);
}

public class PaymentApiService : IPaymentApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentApiService> _logger;

    public PaymentApiService(HttpClient httpClient, ILogger<PaymentApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(int invoiceId)
    {
        try
        {
            var payments = await _httpClient.GetFromJsonAsync<List<Payment>>($"api/payments/invoice/{invoiceId}");
            return (IReadOnlyList<Payment>?)(payments) ?? Array.Empty<Payment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for invoice {InvoiceId}", invoiceId);
            return Array.Empty<Payment>();
        }
    }

    public async Task<Payment?> CreateAsync(Payment payment)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/payments", payment);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Payment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/payments/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {Id}", id);
            return false;
        }
    }
}
