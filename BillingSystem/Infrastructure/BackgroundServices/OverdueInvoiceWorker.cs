using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.Models;
using BillingSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BillingSystem.Infrastructure.Services.Email;

namespace BillingSystem.Infrastructure.BackgroundServices
{
    public class OverdueInvoiceWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueInvoiceWorker> _logger;

        // Run check every 24 hours
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

        public OverdueInvoiceWorker(IServiceProvider serviceProvider, ILogger<OverdueInvoiceWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Invoice Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendOverdueNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing overdue invoices.");
                }

                // Wait for the next interval
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendOverdueNotificationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateTime.Today;

            // Find invoices that are overdue, not paid, and (optionally) haven't been notified recently.
            // For simplicity, we'll just check for overdue status here.
            // In a real app, you might want to flag invoices as "Notified" to avoid spamming.
            var overdueInvoices = await db.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status != "Paid" && 
                            i.DueDate.HasValue && 
                            i.DueDate.Value.Date < today)
                .ToListAsync(stoppingToken);

            _logger.LogInformation($"Found {overdueInvoices.Count} overdue invoices.");

            foreach (var invoice in overdueInvoices)
            {
                if (invoice.Customer == null || string.IsNullOrWhiteSpace(invoice.Customer.Email))
                {
                    continue;
                }

                try
                {
                    var subject = $"Overdue Invoice Reminder - #{invoice.InvoiceNumber}";
                    var content = $@"
                        <p>Dear {invoice.Customer.Name},</p>
                        <p>This is a reminder that invoice <strong>#{invoice.InvoiceNumber}</strong> was due on <span style='color: #d32f2f;'>{invoice.DueDate:yyyy-MM-dd}</span>.</p>
                        <div class='info-box'>
                            <p style='margin: 0;'><strong>Total Amount Due:</strong> {invoice.RemainingAmount:C}</p>
                        </div>
                        <p>Please arrange for payment as soon as possible to avoid any service interruption.</p>";

                    var body = EmailTemplateHelper.GenerateEmailTemplate(
                        "Overdue Invoice Reminder", 
                        content, 
                        null, // You could add a link to view the invoice online here if you had a public URL
                        null
                    );
                    await emailService.SendEmailWithAttachmentAsync(invoice.Customer.Email, subject, body);
                    _logger.LogInformation($"Sent overdue reminder for Invoice #{invoice.InvoiceNumber} to {invoice.Customer.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email for Invoice #{invoice.InvoiceNumber}");
                }
            }
        }
    }
}
