using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Models;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class AdminService : IAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<AdminService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<SystemOverviewDto> GetSystemOverviewAsync()
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today;
            var lastMonth = today.AddMonths(-1);

            // Get all data
            var users = await context.Users.AsNoTracking().ToListAsync();
            var customers = await context.Customers.AsNoTracking().ToListAsync();
            var invoices = await context.Invoices.AsNoTracking().Include(i => i.Payments).ToListAsync();
            var payments = await context.Payments.AsNoTracking().ToListAsync();

            // Calculate totals
            var totalInvoicesAmount = invoices.Sum(i => i.TotalAmount);
            var totalPaymentsAmount = payments.Sum(p => p.Amount);

            // Invoice status breakdown
            var paidInvoices = invoices.Where(i => i.Status == "Paid").ToList();
            var pendingInvoices = invoices.Where(i => i.Status == "Pending" && (!i.DueDate.HasValue || i.DueDate.Value >= today)).ToList();
            var overdueInvoices = invoices.Where(i => i.DueDate.HasValue && i.DueDate.Value < today && i.Status != "Paid" && i.Status != "Cancelled").ToList();

            // Growth calculations (compare to last month)
            var usersLastMonth = users.Count(u => u.CreatedAt < lastMonth);
            var customersLastMonth = customers.Count(c => c.CreatedAt < lastMonth);
            var revenueLastMonth = payments.Where(p => p.Date < lastMonth).Sum(p => p.Amount);

            var userGrowthRate = CalculateGrowthRate(usersLastMonth, users.Count);
            var customerGrowthRate = CalculateGrowthRate(customersLastMonth, customers.Count);
            var revenueGrowthRate = CalculateGrowthRate((double)revenueLastMonth, (double)totalPaymentsAmount);

            // Role distribution
            var adminCount = users.Count(u => u.Role == "Admin");
            var accountantCount = users.Count(u => u.Role == "Accountant");
            var customerUserCount = users.Count(u => u.Role == "Customer");

            return new SystemOverviewDto
            {
                TotalUsers = users.Count,
                TotalCustomers = customers.Count,
                TotalInvoices = invoices.Count,
                TotalInvoicesAmount = totalInvoicesAmount,
                TotalPayments = payments.Count,
                TotalPaymentsAmount = totalPaymentsAmount,

                PaidInvoices = paidInvoices.Count,
                PendingInvoices = pendingInvoices.Count,
                OverdueInvoices = overdueInvoices.Count,
                PaidAmount = paidInvoices.Sum(i => i.TotalAmount),
                PendingAmount = pendingInvoices.Sum(i => i.TotalAmount),
                OverdueAmount = overdueInvoices.Sum(i => i.TotalAmount),

                UserGrowthRate = userGrowthRate,
                CustomerGrowthRate = customerGrowthRate,
                RevenueGrowthRate = revenueGrowthRate,

                AdminCount = adminCount,
                AccountantCount = accountantCount,
                CustomerCount = customerUserCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system overview");
            throw;
        }
    }

    public async Task<IReadOnlyList<ActivityLogDto>> GetRecentActivitiesAsync(int days = 7, int limit = 20)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var activities = new List<ActivityLogDto>();

            // Recent users
            var recentUsers = await context.Users
                .Where(u => u.CreatedAt >= cutoffDate)
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit / 4)
                .AsNoTracking()
                .ToListAsync();

            foreach (var user in recentUsers)
            {
                activities.Add(new ActivityLogDto
                {
                    ActivityType = "UserRegistered",
                    Description = $"New user registered: {user.FullName}",
                    UserName = user.FullName,
                    RelatedEmail = user.Email,
                    Timestamp = user.CreatedAt,
                    Icon = "PersonAdd",
                    Color = "Success"
                });
            }

            // Recent invoices
            var recentInvoices = await context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.CreatedAt >= cutoffDate)
                .OrderByDescending(i => i.CreatedAt)
                .Take(limit / 4)
                .AsNoTracking()
                .ToListAsync();

            foreach (var invoice in recentInvoices)
            {
                activities.Add(new ActivityLogDto
                {
                    ActivityType = "InvoiceCreated",
                    Description = $"Invoice {invoice.InvoiceNumber} created for {invoice.Customer?.Name}",
                    UserName = invoice.Customer?.Name ?? "Unknown",
                    Timestamp = invoice.CreatedAt,
                    Amount = invoice.TotalAmount,
                    Icon = "Receipt",
                    Color = "Primary"
                });
            }

            // Recent payments
            var recentPayments = await context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Customer)
                .Where(p => p.CreatedAt >= cutoffDate)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit / 2)
                .AsNoTracking()
                .ToListAsync();

            foreach (var payment in recentPayments)
            {
                activities.Add(new ActivityLogDto
                {
                    ActivityType = "PaymentReceived",
                    Description = $"Payment received for {payment.Invoice?.InvoiceNumber}",
                    UserName = payment.Invoice?.Customer?.Name ?? "Unknown",
                    Timestamp = payment.CreatedAt,
                    Amount = payment.Amount,
                    Icon = "Payment",
                    Color = "Success"
                });
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetUserGrowthDataAsync(int months = 6)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var startDate = DateTime.Today.AddMonths(-months);

            var users = await context.Users
                .Where(u => u.CreatedAt >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var grouped = users
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Key = $"{g.Key.Year}-{g.Key.Month:00}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Count);

            return grouped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user growth data");
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(int months = 6)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var startDate = DateTime.Today.AddMonths(-months);

            var payments = await context.Payments
                .Where(p => p.Date >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var grouped = payments
                .GroupBy(p => new { p.Date.Year, p.Date.Month })
                .Select(g => new
                {
                    Key = $"{g.Key.Year}-{g.Key.Month:00}",
                    Total = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Total);

            return grouped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            throw;
        }
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();

            // Get database size (SQL Server specific)
            var dbSizeQuery = @"
                SELECT SUM(size) * 8 AS SizeKB
                FROM sys.master_files
                WHERE database_id = DB_ID()";

            long dbSizeKB = 0;
            try
            {
                dbSizeKB = await context.Database.SqlQueryRaw<long>(dbSizeQuery).FirstOrDefaultAsync();
            }
            catch
            {
                // Fallback if query fails
                dbSizeKB = 0;
            }

            // Get record counts
            var usersCount = await context.Users.CountAsync();
            var customersCount = await context.Customers.CountAsync();
            var invoicesCount = await context.Invoices.CountAsync();
            var itemsCount = await context.InvoiceItems.CountAsync();
            var paymentsCount = await context.Payments.CountAsync();

            // Get database version
            var versionQuery = "SELECT @@VERSION";
            var dbVersion = "Unknown";
            try
            {
                dbVersion = await context.Database.SqlQueryRaw<string>(versionQuery).FirstOrDefaultAsync() ?? "Unknown";
                // Extract just the version number
                if (dbVersion.Contains("Microsoft SQL Server"))
                {
                    var parts = dbVersion.Split('-');
                    if (parts.Length > 0)
                        dbVersion = parts[0].Trim();
                }
            }
            catch
            {
                dbVersion = "SQL Server";
            }

            return new SystemHealthDto
            {
                DatabaseSizeKB = dbSizeKB,
                TotalTables = 5,
                UsersCount = usersCount,
                CustomersCount = customersCount,
                InvoicesCount = invoicesCount,
                InvoiceItemsCount = itemsCount,
                PaymentsCount = paymentsCount,
                LastBackupDate = null, // Would need to query backup history
                DatabaseVersion = dbVersion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetUserRoleDistributionAsync()
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();

            var distribution = await context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Role, x => x.Count);

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user role distribution");
            throw;
        }
    }

    private static double CalculateGrowthRate(double oldValue, double newValue)
    {
        if (oldValue == 0)
            return newValue > 0 ? 100 : 0;

        return ((newValue - oldValue) / oldValue) * 100;
    }
}
