using System.Threading.Tasks;

namespace BillingSystem.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string body,
            byte[]? attachmentBytes = null,
            string? attachmentName = null);
    }
}
