using System.Text.Json;
using BillingSystem.Core.Models;
using Microsoft.Extensions.Configuration;

namespace BillingSystem.Infrastructure.Services;

public interface ISystemSettingsService
{
    Task<SystemSettings> GetSettingsAsync();
    Task SaveSettingsAsync(SystemSettings settings);
}

public class SystemSettingsService : ISystemSettingsService
{
    private readonly string _filePath;
    private SystemSettings _currentSettings;

    public SystemSettingsService(IConfiguration configuration)
    {
        var contentRootPath = configuration["ContentRootPath"] ?? Directory.GetCurrentDirectory();
        _filePath = Path.Combine(contentRootPath, "system_settings.json");
        _currentSettings = new SystemSettings(); // Default
    }

    public async Task<SystemSettings> GetSettingsAsync()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                _currentSettings = JsonSerializer.Deserialize<SystemSettings>(json) ?? new SystemSettings();
            }
            catch
            {
                // Ignore errors, use defaults
            }
        }
        return _currentSettings;
    }

    public async Task SaveSettingsAsync(SystemSettings settings)
    {
        _currentSettings = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}
