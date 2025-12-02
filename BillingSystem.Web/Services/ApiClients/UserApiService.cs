using System.Net.Http.Json;
using BillingSystem.Core.Models;
using BillingSystem.Core.Interfaces;

namespace BillingSystem.Web.Services.ApiClients;

public interface IUserApiService
{
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null, string? role = null);
    Task<User?> GetByIdAsync(int id);
    Task<User?> CreateAsync(User user, string password);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
}

public class UserApiService : IUserApiService, IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiService> _logger;

    public UserApiService(HttpClient httpClient, ILogger<UserApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search = null, string? role = null)
    {
        try
        {
            var url = $"api/users?pageIndex={pageIndex}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(role))
                url += $"&role={Uri.EscapeDataString(role)}";

            var response = await _httpClient.GetFromJsonAsync<PagedResult<User>>(url);
            
            return (response?.Items ?? Array.Empty<User>(), response?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged users");
            return (Array.Empty<User>(), 0);
        }
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<User>($"api/users/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Id}", id);
            return null;
        }
    }

    // IUserApiService version - returns Task<User?>
    Task<User?> IUserApiService.CreateAsync(User user, string password)
    {
        return CreateAsyncInternal(user, password);
    }

    // IUserService version - returns Task<User>
    async Task<User> IUserService.CreateAsync(User user, string password)
    {
        var result = await CreateAsyncInternal(user, password);
        return result ?? throw new InvalidOperationException("Failed to create user");
    }

    private async Task<User?> CreateAsyncInternal(User user, string password)
    {
        try
        {
            var request = new { User = user, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/users", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    // IUserApiService version
    async Task<bool> IUserApiService.UpdateAsync(User user)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", user.Id);
            return false;
        }
    }

    // IUserService version
    async Task IUserService.UpdateAsync(User user)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", user.Id);
            throw;
        }
    }

    // IUserApiService version
    async Task<bool> IUserApiService.DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return false;
        }
    }

    // IUserService version
    async Task IUserService.DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            throw;
        }
    }
}

