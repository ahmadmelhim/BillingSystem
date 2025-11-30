namespace BillingSystem.Core.DTOs;

/// <summary>
/// DTO for system health information
/// </summary>
public class SystemHealthDto
{
    public long DatabaseSizeKB { get; set; }
    public int TotalTables { get; set; }
    
    // Record counts per table
    public int UsersCount { get; set; }
    public int CustomersCount { get; set; }
    public int InvoicesCount { get; set; }
    public int InvoiceItemsCount { get; set; }
    public int PaymentsCount { get; set; }
    
    public DateTime? LastBackupDate { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    
    public string DatabaseSizeFormatted => FormatBytes(DatabaseSizeKB * 1024);
    
    // Aliases for Razor compatibility
    public string DatabaseSize => DatabaseSizeFormatted;
    public int TableCount => TotalTables;

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
