namespace BillingSystem.Core.Interfaces;

/// <summary>
/// Service for getting the current authenticated user's information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID from the authentication context
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    Task<int?> GetCurrentUserIdAsync();
}
