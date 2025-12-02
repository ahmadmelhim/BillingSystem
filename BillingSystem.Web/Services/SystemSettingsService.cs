using BillingSystem.Core.Models;

namespace BillingSystem.Web.Services;

public interface ISystemSettingsService
{
    Task<SystemSettings?> GetSettingsAsync();
    Task SaveSettingsAsync(SystemSettings settings);
}

public class SystemSettingsService : ISystemSettingsService
{
    public Task<SystemSettings?> GetSettingsAsync()
    {
        // Placeholder - returns default settings
        return Task.FromResult<SystemSettings?>(new SystemSettings());
    }

    public Task SaveSettingsAsync(SystemSettings settings)
    {
        // Placeholder - does nothing for now
        return Task.CompletedTask;
    }
}
