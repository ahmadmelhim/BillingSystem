using System.Security.Claims;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BillingSystem.Infrastructure.Services.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int?> GetCurrentUserIdAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            return null;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return await Task.FromResult(userId);
        }

        return null;
    }

    public async Task<string?> GetCurrentUserRoleAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            return null;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role);
        return await Task.FromResult(roleClaim?.Value);
    }
}
