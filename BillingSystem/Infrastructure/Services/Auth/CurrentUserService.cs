using System.Security.Claims;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace BillingSystem.Infrastructure.Services.Auth;

/// <summary>
/// Implementation of ICurrentUserService that extracts user ID from authentication state
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public CurrentUserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<int?> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return null;

        if (int.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }
}
