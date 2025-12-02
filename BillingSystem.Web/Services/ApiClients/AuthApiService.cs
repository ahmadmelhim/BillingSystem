using System.Net.Http.Json;
using System.Text.Json;
using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces;

namespace BillingSystem.Web.Services.ApiClients;

public interface IAuthApiService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<UserInfo?> GetCurrentUserAsync();
}

public class AuthApiService : IAuthApiService, IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiService> _logger;
    private UserInfo? _currentUser;

    public event Action? OnAuthStateChanged;

    public AuthApiService(HttpClient httpClient, ILogger<AuthApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    _currentUser = new UserInfo 
                    { 
                        Email = request.Email,
                        FullName = authResponse.FullName ?? request.Email,
                        Role = authResponse.Role ?? "Customer"
                    };
                    OnAuthStateChanged?.Invoke();
                }
                return authResponse;
            }

            _logger.LogWarning("Login failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }

            _logger.LogWarning("Registration failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            throw;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/me");
            
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<UserInfo>();
                return _currentUser;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_currentUser != null)
            return true;

        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        OnAuthStateChanged?.Invoke();
        await Task.CompletedTask;
    }
}
