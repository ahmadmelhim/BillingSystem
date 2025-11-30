using System.Net.Http.Json;
using System.Security.Claims;
using BillingSystem.Core.Models;
using BillingSystem.Core.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BillingSystem.Infrastructure.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string email, string password);
    Task<AuthResponse> RegisterAsync(string fullName, string email, string password, string role);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
    Task<UserInfo?> GetCurrentUserAsync();
    event Action OnAuthStateChanged;
}

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly ILogger<AuthService> _logger;
    private const string TokenKey = "authToken";
    private const string UserKey = "currentUser";

    public AuthService(HttpClient http, IJSRuntime js, ILogger<AuthService> logger)
    {
        _http = http;
        _js = js;
        _logger = logger;
    }

    public event Action? OnAuthStateChanged;

    private void NotifyAuthStateChanged() => OnAuthStateChanged?.Invoke();

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Email}", email);
            
            var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest
            {
                Email = email,
                Password = password
            });

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await SaveAuthDataAsync(authResponse);
                    _logger.LogInformation("Login successful for user: {Email}", email);
                    return authResponse;
                }
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Email}. Status: {StatusCode}", email, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", email);
        }

        return null;
    }

    public async Task<AuthResponse> RegisterAsync(string fullName, string email, string password, string role)
    {
        try
        {
            _logger.LogInformation("Attempting registration for user: {Email}, Role: {Role}", email, role);
            
            var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role
            });

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await SaveAuthDataAsync(authResponse);
                    _logger.LogInformation("Registration successful for user: {Email}", email);
                    return authResponse;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Registration failed for user: {Email}. Status: {StatusCode}, Error: {Error}", 
                    email, response.StatusCode, errorContent);
                    
                if (errorContent.Contains("Email already registered"))
                {
                    throw new InvalidOperationException("EmailAlreadyExists");
                }
                throw new InvalidOperationException("RegistrationFailed");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network error during registration for user: {Email}", email);
            throw new Exception("NetworkError", ex);
        }

        _logger.LogError("Unknown error during registration for user: {Email}", email);
        throw new InvalidOperationException("UnknownError");
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("User logging out");
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", UserKey);
        NotifyAuthStateChanged();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token from localStorage");
            return null;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var userJson = await _js.InvokeAsync<string?>("localStorage.getItem", UserKey);
            if (!string.IsNullOrEmpty(userJson))
            {
                return System.Text.Json.JsonSerializer.Deserialize<UserInfo>(userJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user from localStorage");
        }

        return null;
    }

    private async Task SaveAuthDataAsync(AuthResponse authResponse)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, authResponse.Token);
        
        var userInfo = new UserInfo
        {
            FullName = authResponse.FullName,
            Email = authResponse.Email,
            Role = authResponse.Role
        };
        
        var userJson = System.Text.Json.JsonSerializer.Serialize(userInfo);
        await _js.InvokeVoidAsync("localStorage.setItem", UserKey, userJson);
        NotifyAuthStateChanged();
    }
}

public class UserInfo
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
