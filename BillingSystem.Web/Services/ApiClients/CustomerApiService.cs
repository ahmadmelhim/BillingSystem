using System.Net.Http.Json;
using BillingSystem.Core.Models;
using BillingSystem.Core.Interfaces;

namespace BillingSystem.Web.Services.ApiClients;

public interface ICustomerApiService
{
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null);
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}

public class CustomerApiService : ICustomerApiService, ICustomerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerApiService> _logger;

    public CustomerApiService(HttpClient httpClient, ILogger<CustomerApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null)
    {
        try
        {
            var url = $"api/customers?pageIndex={pageIndex}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _httpClient.GetFromJsonAsync<PagedResult<Customer>>(url);
            
            return (response?.Items ?? Array.Empty<Customer>(), response?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged customers");
            return (Array.Empty<Customer>(), 0);
        }
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Customer>($"api/customers/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {Id}", id);
            return null;
        }
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/customers", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>() 
                ?? throw new InvalidOperationException("Failed to deserialize customer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    public async Task UpdateAsync(Customer customer)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/customers/{customer.Id}", customer);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", customer.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/customers/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            throw;
        }
    }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
}
