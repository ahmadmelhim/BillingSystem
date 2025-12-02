using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Configuration;

namespace BillingSystem.Infrastructure.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string body,
            byte[]? attachmentBytes = null,
            string? attachmentName = null)
        {
            try
            {
                _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", to, subject);

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromAddress, _settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentName))
                {
                    var stream = new MemoryStream(attachmentBytes);
                    message.Attachments.Add(new Attachment(stream, attachmentName, "application/pdf"));
                    _logger.LogDebug("Added attachment: {AttachmentName}", attachmentName);
                }

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} with subject: {Subject}", to, subject);
                throw;
            }
        }
    }
}

