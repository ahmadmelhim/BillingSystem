using BillingSystem.Core.DTOs;

namespace BillingSystem.Core.Interfaces;

public interface IAuthService
{
    event Action? OnAuthStateChanged;
    
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<UserInfo?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task LogoutAsync();
}
