using BillingSystem.Core.DTOs;

namespace BillingSystem.Core.Interfaces;

/// <summary>
/// Service for Admin-only operations and system-wide analytics
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Gets comprehensive system overview statistics
    /// </summary>
    Task<SystemOverviewDto> GetSystemOverviewAsync();

    /// <summary>
    /// Gets recent system activities
    /// </summary>
    /// <param name="days">Number of days to look back</param>
    /// <param name="limit">Maximum number of activities to return</param>
    Task<IReadOnlyList<ActivityLogDto>> GetRecentActivitiesAsync(int days = 7, int limit = 20);

    /// <summary>
    /// Gets user growth data for the last N months
    /// </summary>
    Task<Dictionary<string, int>> GetUserGrowthDataAsync(int months = 6);

    /// <summary>
    /// Gets revenue analytics for the last N months
    /// </summary>
    Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(int months = 6);

    /// <summary>
    /// Gets system health information
    /// </summary>
    Task<SystemHealthDto> GetSystemHealthAsync();

    /// <summary>
    /// Gets user role distribution
    /// </summary>
    Task<Dictionary<string, int>> GetUserRoleDistributionAsync();
}
