using System.Security.Claims;
using System.Text.Json;
using BillingSystem.Core.Models;
using BillingSystem.Core.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace BillingSystem.Infrastructure.Services.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly IAuthService _authService;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(IJSRuntime js, IAuthService authService)
    {
        _js = js;
        _authService = authService;
        
        // Subscribe to AuthService event
        _authService.OnAuthStateChanged += AuthStateChanged;
    }

    private void AuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(_anonymous);

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null) return Enumerable.Empty<Claim>();

        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
    
    public void Dispose()
    {
        _authService.OnAuthStateChanged -= AuthStateChanged;
    }
}

