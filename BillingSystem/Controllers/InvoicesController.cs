using System.Threading.Tasks;
using BillingSystem.Core.Models;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Cookies,Bearer")]
    public class InvoicesController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IInvoiceService _invoiceService;
        private readonly IEmailService _emailService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IPdfService pdfService,
            IInvoiceService invoiceService,
            IEmailService emailService,
            ILogger<InvoicesController> logger)
        {
            _pdfService = pdfService;
            _invoiceService = invoiceService;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/invoices/5/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(int id)
        {
            try
            {
                _logger.LogInformation("Generating PDF for invoice {InvoiceId}", id);

                var invoice = await _invoiceService.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found for PDF generation", id);
                    return NotFound();
                }

                var bytes = await _pdfService.GenerateInvoicePdfAsync(id);
                var fileName = $"Invoice-{invoice.InvoiceNumber}.pdf";

                _logger.LogInformation("PDF generated successfully for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", id);
                return StatusCode(500, "Error generating PDF");
            }
        }

        // POST: api/invoices/5/send-email
        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to send email for invoice {InvoiceId}", id);

                var invoice = await _invoiceService.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found for email sending", id);
                    return NotFound("الفاتورة غير موجودة.");
                }

                if (invoice.Customer == null || string.IsNullOrWhiteSpace(invoice.Customer.Email))
                {
                    _logger.LogWarning("No email found for customer of invoice {InvoiceId}", id);
                    return BadRequest("لا يوجد بريد إلكتروني مسجل للعميل.");
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);
                var fileName = $"Invoice-{invoice.InvoiceNumber}.pdf";

                var subject = $"فاتورة رقم {invoice.InvoiceNumber}";
                var body =
                    $"عزيزي/عزيزتي {invoice.Customer.Name},\n\n" +
                    $"مرفق فاتورتك بتاريخ {invoice.DateIssued:yyyy-MM-dd} بمبلغ {invoice.TotalAmount:0.00}.\n\n" +
                    $"مع التحية,\nBilling System";

                await _emailService.SendEmailWithAttachmentAsync(
                    invoice.Customer.Email,
                    subject,
                    body,
                    pdfBytes,
                    fileName);

                _logger.LogInformation("Email sent successfully for invoice {InvoiceNumber} to {Email}", 
                    invoice.InvoiceNumber, invoice.Customer.Email);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for invoice {InvoiceId}", id);
                return StatusCode(500, $"Error while sending email: {ex.Message}");
            }
        }
    }
}
